<script setup lang="ts">
import { youtubeThumbnail } from '@/shared/utils/youtube'
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import 'leaflet.markercluster'
import 'leaflet.markercluster/dist/MarkerCluster.css'
import type { PublicContent } from '../../api/contentApi'

// Content titles etc. are user-authored and injected into HTML strings (popup / marker icon)
function esc(value: unknown): string {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')
}

function hasCoords(c: PublicContent) {
  return Boolean(c.latitude && c.longitude)
}

interface Props {
  contents: PublicContent[]
  loading?: boolean
}

const props = defineProps<Props>()
const router = useRouter()

const mapContainer = ref<HTMLElement | null>(null)
const selectedContentId = ref<string | null>(null)
let map: L.Map | null = null
let markerGroup: L.MarkerClusterGroup | null = null
const markersMap = new Map<string, L.Marker>()
const markerDistrict = new WeakMap<L.Marker, string>()

// Kanchanaburi Province Geographic Bounding Box
const KANCHANABURI_BOUNDS: L.LatLngBoundsExpression = [
  [13.70, 98.00], // South-West Boundary
  [15.85, 100.00], // North-East Boundary
]



function initMap() {
  if (!mapContainer.value || map) return

  map = L.map(mapContainer.value, {
    zoomControl: false,
    minZoom: 9,
    maxZoom: 18,
    maxBounds: KANCHANABURI_BOUNDS,
    maxBoundsViscosity: 0.4,
  }).setView([14.0227, 99.5328], 10)

  L.control.zoom({ position: 'topright' }).addTo(map)

  // Plain OSM tiles (no API key). Softened toward the theme via CSS filter on .leaflet-tile-pane
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors',
    maxZoom: 19,
    bounds: KANCHANABURI_BOUNDS,
    keepBuffer: 8,
    updateWhenIdle: false,
    updateWhenZooming: false,
  }).addTo(map)

  markerGroup = L.markerClusterGroup({
    showCoverageOnHover: false,
    maxClusterRadius: 55,
    spiderfyOnMaxZoom: true,
    iconCreateFunction: (cluster) => {
      const count = cluster.getChildCount()
      // Label the cluster with its most common district instead of a bare number
      const tally = new Map<string, number>()
      cluster.getAllChildMarkers().forEach((m) => {
        const name = markerDistrict.get(m)
        if (name) tally.set(name, (tally.get(name) ?? 0) + 1)
      })
      const top = [...tally.entries()].sort((a, b) => b[1] - a[1])[0]?.[0]
      const label = top ? `อ.${top}` : 'หลายพื้นที่'
      return L.divIcon({
        className: 'custom-content-marker',
        html: `<div class="content-cluster"><span class="content-cluster-name">${esc(label)}</span><span class="content-cluster-count">${count}</span></div>`,
        iconSize: [0, 0],
        iconAnchor: [0, 0],
      })
    },
  }).addTo(map)
  renderMarkers()
}

function createMarkerIcon(imageUrl: string, isSelected: boolean, title: string) {
  const safeTitle = esc(title || 'คอนเทนต์')
  const safeUrl = esc(imageUrl)
  const ringClass = isSelected
    ? 'ring-4 ring-[#D96C2C] scale-125 z-50 shadow-2xl'
    : 'ring-2 ring-white/90 hover:ring-[#D96C2C] hover:scale-115 shadow-lg'

  return L.divIcon({
    className: 'custom-content-marker',
    html: `
      <div class="relative flex items-center justify-center cursor-pointer transition-all duration-300">
        <!-- Photo Circular Frame -->
        <div class="relative h-12 w-12 rounded-full overflow-hidden border-2 border-white bg-slate-900 ${ringClass} transition-all duration-300">
          <img src="${safeUrl}" alt="${safeTitle}" class="h-full w-full object-cover" />
          <div class="absolute inset-0 bg-gradient-to-t from-black/50 via-transparent to-transparent"></div>
          <!-- Tiny Orange Play Icon Indicator in Bottom-Right -->
          <div class="absolute bottom-0 right-0 h-4 w-4 rounded-full bg-[#D96C2C] text-white flex items-center justify-center text-[9px] shadow-sm border border-white">
            <i class="mdi mdi-play"></i>
          </div>
        </div>
        <!-- Pointer Pin Tip -->
        <div class="absolute -bottom-1 h-2.5 w-2.5 bg-[#D96C2C] rotate-45 border-r border-b border-white shadow-xs"></div>
      </div>
    `,
    iconSize: [48, 52],
    iconAnchor: [24, 50],
    popupAnchor: [0, -48],
  })
}

function updateMarkerIcons(newSelectedId: string | null) {
  markersMap.forEach((marker, id) => {
    const cnt = props.contents.find((c) => c.contentId === id)
    if (cnt) {
      const coverUrl =
        youtubeThumbnail(cnt.youtubeUrl) ||
        'https://images.unsplash.com/photo-1606744888344-493238951221?auto=format&fit=crop&w=400&q=80'
      marker.setIcon(createMarkerIcon(coverUrl, id === newSelectedId, cnt.title))
    }
  })
}

function renderMarkers() {
  if (!map || !markerGroup) return
  markerGroup.clearLayers()
  markersMap.clear()

  const validContents = props.contents.filter(hasCoords)
  const bounds = L.latLngBounds([])

  validContents.forEach((cnt) => {
    const lat = cnt.latitude!
    const lng = cnt.longitude!
    bounds.extend([lat, lng])

    const isSelected = selectedContentId.value === cnt.contentId
    const coverUrl =
      youtubeThumbnail(cnt.youtubeUrl) ||
      'https://images.unsplash.com/photo-1606744888344-493238951221?auto=format&fit=crop&w=400&q=80'
    const icon = createMarkerIcon(coverUrl, isSelected, cnt.title)

    const popupHtml = `
      <div class="p-1 w-[240px] text-slate-900 space-y-2.5 font-sans">
        <div class="relative h-32 w-full rounded-2xl overflow-hidden bg-slate-900 shadow-xs">
          <img src="${esc(coverUrl)}" alt="${esc(cnt.title)}" class="h-full w-full object-cover" />
          <div class="absolute inset-0 bg-gradient-to-t from-slate-950/80 via-transparent to-transparent"></div>
          <span class="absolute top-2 left-2 px-2.5 py-0.5 rounded-full text-[10px] font-black bg-[#D96C2C] text-white shadow-md">
            ${esc(cnt.contentCategoryName || 'คอนเทนต์')}
          </span>
        </div>
        <div class="space-y-1">
          <h4 class="font-black text-slate-950 text-sm leading-tight line-clamp-1">${esc(cnt.title)}</h4>
          <p class="text-[11px] font-bold text-slate-600 flex items-center gap-1">
            <i class="mdi mdi-map-marker text-[#D96C2C]"></i>
            อ.${esc(cnt.districtName || 'สังขละบุรี')} ${cnt.subDistrictName ? 'ต.' + esc(cnt.subDistrictName) : ''}
          </p>
        </div>
        <a
          href="${router.resolve(`/contents/${encodeURIComponent(cnt.contentId)}`).href}"
          data-content-id="${esc(cnt.contentId)}"
          style="color: #ffffff !important;"
          class="flex items-center justify-center gap-1.5 w-full py-2 rounded-full bg-[#D96C2C] hover:bg-[#BF5720] !text-white text-xs font-bold transition-all shadow-md active:scale-95 text-decoration-none border border-[#D96C2C]"
        >
          <span style="color: #ffffff !important;">ดูคอนเทนต์นี้</span>
          <i class="mdi mdi-arrow-right text-xs text-white"></i>
        </a>
      </div>
    `

    const marker = L.marker([lat, lng], { icon }).bindPopup(popupHtml, {
      maxWidth: 270,
      minWidth: 260,
      className: 'custom-leaflet-popup',
    })

    marker.on('click', () => {
      selectedContentId.value = cnt.contentId
    })

    // ลิงก์ในป๊อปอัปเป็น HTML ธรรมดา: ให้ไปหน้าผ่าน Vue Router (ไม่โหลดหน้าใหม่ และคง base path)
    marker.on('popupopen', (event) => {
      const link = event.popup.getElement()?.querySelector<HTMLAnchorElement>('a[data-content-id]')
      link?.addEventListener('click', (e) => {
        e.preventDefault()
        router.push(`/contents/${encodeURIComponent(cnt.contentId)}`)
      })
    })

    if (markerGroup) {
      if (cnt.districtName) markerDistrict.set(marker, cnt.districtName)
      markerGroup.addLayer(marker)
      markersMap.set(cnt.contentId, marker)
    }
  })

  if (validContents.length > 0) {
    map.fitBounds(bounds, { padding: [50, 50], maxZoom: 13 })
  }
}

function selectContent(cnt: PublicContent) {
  selectedContentId.value = cnt.contentId
  if (hasCoords(cnt) && map && markerGroup) {
    const marker = markersMap.get(cnt.contentId)
    if (marker) {
      // Expands/zooms past any cluster that currently hides this marker, then opens it
      markerGroup.zoomToShowLayer(marker, () => marker.openPopup())
    }
  }
}

watch(selectedContentId, (newId) => {
  updateMarkerIcons(newId)
})

watch(
  () => props.contents,
  () => {
    void nextTick(() => {
      renderMarkers()
    })
  },
  { deep: true },
)

onMounted(() => {
  void nextTick(() => {
    initMap()
  })
})

onBeforeUnmount(() => {
  if (map) {
    map.remove()
    map = null
  }
})
</script>

<template>
  <div class="overflow-hidden rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white shadow-lg shadow-[#D96C2C]/5">
    <!-- MAP HEADER TOOLBAR -->
    <div class="flex flex-wrap items-center justify-between gap-3 border-b-2 border-[#E8D9C9] bg-[#FFF9F2] px-5 py-4 sm:px-6">
      <div class="flex items-center gap-3">
        <div class="flex h-11 w-11 items-center justify-center rounded-2xl bg-[#D96C2C] font-bold text-white shadow-md">
          <i class="mdi mdi-map-marker-radius text-xl"></i>
        </div>
        <div>
          <h3 class="font-black text-[#332820] text-base sm:text-lg">แผนที่พิกัดคอนเทนต์ (เฉพาะกาญจนบุรี)</h3>
          <p class="text-xs text-[#786B62] font-bold">คลิกเลือกหมุดบนแผนที่ หรือเลือกรายการด้านข้างเพื่อดูเรื่องราว</p>
        </div>
      </div>
      <span class="inline-flex items-center gap-1.5 text-xs font-bold text-orange-950 bg-orange-100/70 border border-orange-300 px-3.5 py-1.5 rounded-full shadow-2xs">
        <i class="mdi mdi-map-marker text-[#D96C2C] text-sm"></i>
        <span>{{ contents.filter(hasCoords).length }} หมุดในจังหวัดกาญจนบุรี</span>
      </span>
    </div>

    <!-- MAIN INTERACTIVE SPLIT CONTAINER -->
    <div class="relative grid grid-cols-1 lg:h-[620px] lg:grid-cols-12">
      <!-- SIDEBAR LIST (4 COLS) WITH EQUAL CARDS -->
      <div class="scrollbar-thin h-full max-h-[320px] space-y-3 overflow-y-auto border-b-2 border-[#E8D9C9] bg-[#F7F0E6] p-4 lg:col-span-4 lg:max-h-none lg:border-r-2 lg:border-b-0">
        <div class="text-xs font-black text-[#D96C2C] uppercase tracking-wider px-1">
          คอนเทนต์ทั้งหมด ({{ contents.length }})
        </div>

        <div v-if="!contents.length" class="text-center py-12 text-[#786B62] text-xs font-bold">
          ไม่พบข้อมูลคอนเทนต์ในระบบ
        </div>

        <div
          v-for="cnt in contents"
          :key="cnt.contentId"
          class="h-[96px] w-full p-3 rounded-2xl bg-white border-2 transition-all duration-200 cursor-pointer shadow-2xs hover:-translate-y-0.5 flex items-center gap-3.5 group shrink-0"
          :class="
            selectedContentId === cnt.contentId
              ? 'border-[#D96C2C] ring-2 ring-orange-500/20 bg-orange-50/50 shadow-md'
              : 'border-[#E8D9C9] hover:border-[#D96C2C] hover:shadow-xs'
          "
          @click="selectContent(cnt)"
        >
          <!-- Thumbnail (Fixed 56x56) -->
          <div class="h-14 w-14 rounded-xl overflow-hidden bg-slate-900 shrink-0 border border-[#E8D9C9] relative">
            <img :src="youtubeThumbnail(cnt.youtubeUrl) || 'https://images.unsplash.com/photo-1606744888344-493238951221?auto=format&fit=crop&w=400&q=80'" :alt="cnt.title" class="h-full w-full object-cover group-hover:scale-110 transition duration-300" />
          </div>

          <!-- Info (Flex 1) -->
          <div class="flex-1 min-w-0 flex flex-col justify-center space-y-0.5">
            <h4 class="font-black text-[#332820] text-xs sm:text-sm truncate group-hover:text-[#D96C2C] transition leading-tight">
              {{ cnt.title }}
            </h4>
            <p class="text-[11px] text-[#786B62] font-bold truncate flex items-center gap-1">
              <i class="mdi mdi-map-marker text-[#D96C2C] shrink-0"></i>
              <span>อ.{{ cnt.districtName || 'สังขละบุรี' }}</span>
              <span
                v-if="!hasCoords(cnt)"
                class="ml-1 shrink-0 rounded-full bg-[#E8D9C9] px-2 py-px text-[10px] text-[#786B62]"
              >
                ไม่มีพิกัด
              </span>
            </p>
            <div class="pt-0.5">
              <span
                class="inline-block text-[10px] font-bold text-orange-900 bg-orange-50 px-2.5 py-0.5 rounded-full border border-orange-200 truncate max-w-full"
              >
                {{ cnt.contentCategoryName || 'เรื่องราว' }}
              </span>
            </div>
          </div>

          <!-- Action Arrow (Capsule Pill) -->
          <div
            class="h-8 w-8 rounded-full bg-[#FFF9F2] group-hover:bg-[#D96C2C] group-hover:text-white text-[#786B62] border border-[#E8D9C9] flex items-center justify-center shrink-0 transition shadow-2xs"
          >
            <i class="mdi mdi-chevron-right text-base"></i>
          </div>
        </div>
      </div>

      <!-- MAP CONTAINER (8 COLS) -->
      <div class="relative h-[420px] lg:col-span-8 lg:h-full">
        <div ref="mapContainer" class="h-full w-full z-0"></div>

        <!-- Loading overlay -->
        <Transition
          enter-active-class="transition-opacity duration-200"
          leave-active-class="transition-opacity duration-300"
          enter-from-class="opacity-0"
          leave-to-class="opacity-0"
        >
          <div
            v-if="loading"
            class="absolute inset-0 z-[1000] flex items-center justify-center bg-[#F7F0E6]/70 backdrop-blur-[2px]"
            role="status"
            aria-live="polite"
          >
            <div
              class="flex items-center gap-3 rounded-full border-2 border-[#E8D9C9] bg-white px-5 py-3 text-sm font-bold text-[#332820] shadow-lg"
            >
              <i class="mdi mdi-loading mdi-spin text-xl text-[#D96C2C]"></i>
              <span>กำลังโหลดหมุดบนแผนที่...</span>
            </div>
          </div>
        </Transition>
      </div>
    </div>
  </div>
</template>

<style>
/* Custom Marker Transparent Wrapper */
.custom-content-marker {
  background: transparent !important;
  border: none !important;
}

/* Cluster bubble (brand orange) */
.content-cluster {
  position: absolute;
  left: 0;
  top: 0;
  transform: translate(-50%, -50%);
  display: flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
  padding: 5px 6px 5px 12px;
  border-radius: 9999px;
  background: #d96c2c;
  color: #fff;
  font-weight: 900;
  font-size: 13px;
  line-height: 1;
  border: 2px solid #fff;
  box-shadow:
    0 0 0 5px rgba(217, 108, 44, 0.25),
    0 8px 16px rgba(51, 40, 32, 0.25);
  transition: transform 0.2s;
}
.content-cluster:hover {
  transform: translate(-50%, -50%) scale(1.08);
}
.content-cluster-count {
  min-width: 22px;
  padding: 3px 6px;
  border-radius: 9999px;
  background: #fff;
  color: #d96c2c;
  font-size: 12px;
  text-align: center;
}

/* Warm, slightly muted basemap to sit better with the cream/orange theme */
.leaflet-tile-pane {
  filter: saturate(0.8) sepia(0.12) brightness(1.02);
}

/* Rounded zoom buttons to match the theme */
.leaflet-bar {
  border: none !important;
  border-radius: 1rem !important;
  overflow: hidden;
  box-shadow: 0 6px 16px rgba(51, 40, 32, 0.18) !important;
}
.leaflet-bar a {
  color: #332820 !important;
  border-bottom: 1px solid #e8d9c9 !important;
  transition:
    background 0.15s,
    color 0.15s;
}
.leaflet-bar a:hover {
  background: #d96c2c !important;
  color: #fff !important;
}

/* Custom Leaflet Popup Overrides */
.leaflet-popup-content-wrapper {
  padding: 0 !important;
  border-radius: 1.25rem !important;
  overflow: hidden !important;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.15), 0 8px 10px -6px rgba(0, 0, 0, 0.1) !important;
  border: 2px solid #E8D9C9 !important;
}
.leaflet-popup-content {
  margin: 12px !important;
  width: 240px !important;
}
.leaflet-container a.leaflet-popup-close-button {
  top: 8px !important;
  right: 8px !important;
  color: #ffffff !important;
  background: rgba(0, 0, 0, 0.4) !important;
  border-radius: 50% !important;
  width: 22px !important;
  height: 22px !important;
  line-height: 20px !important;
  text-align: center !important;
  font-size: 14px !important;
  backdrop-filter: blur(4px) !important;
}
.leaflet-container a.leaflet-popup-close-button:hover {
  background: rgba(0, 0, 0, 0.7) !important;
  color: #F2A65A !important;
}
</style>
