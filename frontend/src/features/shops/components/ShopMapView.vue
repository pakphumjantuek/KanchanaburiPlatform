<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import 'leaflet.markercluster'
import 'leaflet.markercluster/dist/MarkerCluster.css'
import type { Shop } from '@/features/shops/shared/types/shop'

interface Props {
  shops: Shop[]
  loading?: boolean
}

const props = defineProps<Props>()
const router = useRouter()

const mapContainer = ref<HTMLElement | null>(null)
const selectedShopId = ref<string | null>(null)

let map: L.Map | null = null
let markerGroup: L.MarkerClusterGroup | null = null

const markersMap = new Map<string, L.Marker>()
const shopsById = new Map<string, Shop>()
const markerDistrict = new WeakMap<L.Marker, string>()

// ขอบเขตจังหวัดกาญจนบุรี
const KANCHANABURI_BOUNDS: L.LatLngBoundsExpression = [
  [13.7, 98.0],
  [15.85, 100.0],
]

const rawApiUrl = (import.meta.env.VITE_API_URL as string | undefined) ?? 'https://localhost:7289'
const apiOrigin = rawApiUrl.startsWith('http')
  ? rawApiUrl.replace(/\/api$/, '')
  : 'https://localhost:7289'

// Shop names etc. are merchant-authored and are injected into HTML strings below
function esc(value: unknown): string {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')
}

function hasCoords(shop: Shop) {
  return Number.isFinite(shop.latitude) && Number.isFinite(shop.longitude)
}

/** รองรับทั้ง https://... และ /uploads/shops/xxx.jpg ; ไม่มีรูปจะได้ '' (UI แสดงไอคอนแทน) */
function resolveImageUrl(url?: string) {
  if (!url?.trim()) return ''
  if (url.startsWith('http://') || url.startsWith('https://')) return url
  return url.startsWith('/') ? `${apiOrigin}${url}` : `${apiOrigin}/${url}`
}

const pinCount = computed(() => props.shops.filter(hasCoords).length)
const missingCoordsCount = computed(() => props.shops.length - pinCount.value)

function initMap() {
  if (!mapContainer.value || map) return

  map = L.map(mapContainer.value, {
    zoomControl: false,
    minZoom: 9,
    maxZoom: 18,
    maxBounds: KANCHANABURI_BOUNDS,
    maxBoundsViscosity: 0.4,
    zoomAnimation: true,
    fadeAnimation: false,
    markerZoomAnimation: false,
  }).setView([14.0227, 99.5328], 10)

  L.control.zoom({ position: 'topright' }).addTo(map)

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors',
    maxZoom: 19,
    bounds: KANCHANABURI_BOUNDS,
    keepBuffer: 4,
    updateWhenIdle: true,
    updateWhenZooming: false,
  }).addTo(map)

  markerGroup = L.markerClusterGroup({
    showCoverageOnHover: false,
    maxClusterRadius: 55,
    spiderfyOnMaxZoom: true,
    iconCreateFunction: (cluster) => {
      // ติดชื่ออำเภอที่มีร้านมากที่สุดในกลุ่ม แทนตัวเลขล้วน
      const tally = new Map<string, number>()
      cluster.getAllChildMarkers().forEach((m) => {
        const name = markerDistrict.get(m)
        if (name) tally.set(name, (tally.get(name) ?? 0) + 1)
      })
      const top = [...tally.entries()].sort((a, b) => b[1] - a[1])[0]?.[0]
      const label = top ? `อ.${top}` : 'หลายพื้นที่'
      return L.divIcon({
        className: 'shop-marker-wrapper',
        html: `<div class="shop-cluster"><span class="shop-cluster-name">${esc(label)}</span><span class="shop-cluster-count">${cluster.getChildCount()}</span></div>`,
        iconSize: [0, 0],
        iconAnchor: [0, 0],
      })
    },
  }).addTo(map)

  // ลิงก์ใน popup ใช้ router (ไม่โหลดหน้าใหม่ทั้งหน้า)
  map.on('popupopen', (e) => {
    const link = e.popup.getElement()?.querySelector<HTMLAnchorElement>('a[data-router-link]')
    link?.addEventListener('click', (ev) => {
      if (ev.metaKey || ev.ctrlKey || ev.shiftKey) return
      ev.preventDefault()
      void router.push(link.getAttribute('href') || '/shops')
    })
  })

  renderMarkers()
}

function createMarkerIcon(shop: Shop, isSelected: boolean) {
  const cover = resolveImageUrl(shop.coverImageUrl)
  const ring = isSelected
    ? 'ring-4 ring-[#D96C2C] scale-125 shadow-2xl'
    : 'ring-2 ring-white/90 hover:ring-[#D96C2C] hover:scale-110 shadow-lg'

  return L.divIcon({
    className: 'shop-marker-wrapper',
    html: `
      <div class="relative flex items-center justify-center cursor-pointer transition-all duration-300">
        <div class="relative h-12 w-12 overflow-hidden rounded-full border-2 border-white bg-[#D96C2C] transition-all duration-300 ${ring}">
          ${
            cover
              ? `<img src="${esc(cover)}" alt="${esc(shop.shopName)}" class="h-full w-full object-cover" />`
              : '<div class="flex h-full w-full items-center justify-center text-white"><i class="mdi mdi-storefront text-xl"></i></div>'
          }
          <div class="absolute inset-0 bg-gradient-to-t from-black/40 via-transparent to-transparent"></div>
          <div class="absolute bottom-0 right-0 flex h-4 w-4 items-center justify-center rounded-full border border-white bg-[#D96C2C] text-[9px] text-white shadow-sm">
            <i class="mdi mdi-storefront"></i>
          </div>
        </div>
        <div class="absolute -bottom-1 h-2.5 w-2.5 rotate-45 border-r border-b border-white bg-[#D96C2C]"></div>
      </div>
    `,
    iconSize: [48, 52],
    iconAnchor: [24, 50],
    popupAnchor: [0, -48],
  })
}

function createPopupHtml(shop: Shop) {
  const cover = resolveImageUrl(shop.coverImageUrl)
  const phone = shop.phone?.replace(/[^\d+]/g, '')
  const dirUrl = `https://www.google.com/maps/dir/?api=1&destination=${shop.latitude},${shop.longitude}`
  const area = [shop.districtName ? `อ.${esc(shop.districtName)}` : '', shop.subDistrictName ? `ต.${esc(shop.subDistrictName)}` : '']
    .filter(Boolean)
    .join(' ')

  return `
    <div class="p-1 w-[240px] space-y-2.5 font-sans text-[#332820]">
      <div class="relative h-32 w-full overflow-hidden rounded-2xl bg-[#171412]">
        ${
          cover
            ? `<img src="${esc(cover)}" alt="${esc(shop.shopName)}" loading="lazy" decoding="async" class="h-full w-full object-cover" />`
            : `<div class="flex h-full w-full items-center justify-center bg-gradient-to-br from-[#D96C2C] via-[#BF5720] to-[#332820] text-white"><i class="mdi mdi-storefront-outline text-5xl opacity-40"></i></div>`
        }
        <div class="absolute inset-0 bg-gradient-to-t from-[#171412]/70 via-transparent to-transparent"></div>
        <span class="absolute top-2 left-2 rounded-full bg-[#D96C2C] px-2.5 py-0.5 text-[10px] font-black text-white shadow-md">
          ${esc(shop.categoryName || 'ร้านค้าชุมชน')}
        </span>
      </div>

      <div class="space-y-1">
        <h4 class="line-clamp-1 text-sm leading-tight font-black text-[#332820]">${esc(shop.shopName)}</h4>
        ${area ? `<p class="flex items-center gap-1 text-[11px] font-semibold text-[#786B62]"><i class="mdi mdi-map-marker text-[#D96C2C]"></i>${area}</p>` : ''}
      </div>

      <div class="flex gap-2">
        <a
          href="/shops/${encodeURIComponent(shop.shopId)}"
          data-router-link
          style="color:#ffffff !important;"
          class="flex flex-1 items-center justify-center gap-1.5 rounded-xl border border-[#D96C2C] bg-[#D96C2C] py-2.5 text-xs font-black text-white shadow-md transition-all hover:bg-[#BF5720] active:scale-95"
        >
          <span style="color:#ffffff !important;">เยี่ยมชมร้านค้า</span>
          <i class="mdi mdi-arrow-right text-xs text-white"></i>
        </a>
        <a
          href="${esc(dirUrl)}"
          target="_blank"
          rel="noopener noreferrer"
          title="นำทางด้วย Google Maps"
          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border-2 border-[#E8D9C9] bg-[#FFF9F2] text-[#D96C2C] transition hover:border-[#D96C2C]"
        >
          <i class="mdi mdi-directions text-lg"></i>
        </a>
        ${
          phone
            ? `<a href="tel:${esc(phone)}" title="โทร" class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border-2 border-[#E8D9C9] bg-[#FFF9F2] text-[#D96C2C] transition hover:border-[#D96C2C]"><i class="mdi mdi-phone-outline text-lg"></i></a>`
            : ''
        }
      </div>
    </div>
  `
}

function setSelected(newId: string | null) {
  const oldId = selectedShopId.value
  selectedShopId.value = newId
  const oldShop = oldId ? shopsById.get(oldId) : undefined
  const newShop = newId ? shopsById.get(newId) : undefined
  if (oldId && oldId !== newId && oldShop) markersMap.get(oldId)?.setIcon(createMarkerIcon(oldShop, false))
  if (newId && newShop) markersMap.get(newId)?.setIcon(createMarkerIcon(newShop, true))
}

function scrollSidebarTo(shopId: string) {
  document
    .getElementById(`shop-map-item-${shopId}`)
    ?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
}

/** เลือกร้านจากรายการด้านข้าง */
function selectShop(shop: Shop) {
  if (!hasCoords(shop) || !map || !markerGroup) return // ไม่มีพิกัด: ไม่มีอะไรให้โชว์บนแผนที่
  setSelected(shop.shopId)
  map.stop()
  const marker = markersMap.get(shop.shopId)
  if (!marker) return
  // ซูม/แตกกลุ่มจนเห็นหมุดนี้ แล้วเปิด popup
  markerGroup.zoomToShowLayer(marker, () => {
    map?.flyTo(marker.getLatLng(), Math.max(map.getZoom(), 14), { duration: 0.4 })
    marker.openPopup()
  })
}

function renderMarkers() {
  if (!map || !markerGroup) return

  markerGroup.clearLayers()
  markersMap.clear()
  shopsById.clear()

  const validShops = props.shops.filter(hasCoords)
  if (!validShops.length) return

  const bounds = L.latLngBounds([])

  validShops.forEach((shop) => {
    const lat = shop.latitude!
    const lng = shop.longitude!
    bounds.extend([lat, lng])

    const marker = L.marker([lat, lng], {
      icon: createMarkerIcon(shop, selectedShopId.value === shop.shopId),
      riseOnHover: true,
    }).bindPopup(createPopupHtml(shop), {
      maxWidth: 270,
      minWidth: 260,
      className: 'custom-leaflet-popup',
    })

    marker.on('click', () => {
      setSelected(shop.shopId)
      scrollSidebarTo(shop.shopId)
    })

    if (shop.districtName) markerDistrict.set(marker, shop.districtName)
    markerGroup?.addLayer(marker)
    markersMap.set(shop.shopId, marker)
    shopsById.set(shop.shopId, shop)
  })

  // เลื่อนกรอบให้พอดีกับผลลัพธ์ทุกครั้งที่ชุดข้อมูลเปลี่ยน (เช่น หลังเลือกอำเภอ/หมวดหมู่)
  if (bounds.isValid()) {
    map.fitBounds(bounds, { padding: [50, 50], maxZoom: 13, animate: false })
  }
}

// ดูเฉพาะ reference ของ array (parent กำหนด shops.value ใหม่ทุกครั้งที่โหลด)
watch(
  () => props.shops,
  () => {
    void nextTick(renderMarkers)
  },
)

onMounted(() => {
  void nextTick(initMap)
})

onBeforeUnmount(() => {
  markersMap.clear()
  shopsById.clear()
  markerGroup?.clearLayers()
  markerGroup = null
  if (map) {
    map.remove()
    map = null
  }
})
</script>

<template>
  <div
    class="overflow-hidden rounded-[1.75rem] border-2 border-[#E8D9C9] bg-[#FFF9F2] shadow-lg shadow-[#D96C2C]/5"
  >
    <!-- HEADER -->
    <div
      class="flex flex-wrap items-center justify-between gap-3 border-b-2 border-[#E8D9C9] bg-[#FFF9F2] px-5 py-4 sm:px-6"
    >
      <div class="flex items-center gap-3">
        <div
          class="flex h-11 w-11 items-center justify-center rounded-2xl bg-[#D96C2C] font-bold text-white shadow-md"
        >
          <i class="mdi mdi-map-marker-radius text-xl"></i>
        </div>
        <div>
          <h3 class="text-base font-black text-[#332820] sm:text-lg">แผนที่ร้านค้าชุมชนกาญจนบุรี</h3>
          <p class="text-xs font-medium text-[#786B62]">
            คลิกเลือกหมุดบนแผนที่ หรือเลือกร้านค้าด้านข้าง
          </p>
        </div>
      </div>

      <div class="flex flex-wrap items-center gap-2">
        <span
          class="inline-flex items-center gap-1.5 rounded-full border border-orange-300 bg-orange-100/70 px-3.5 py-1.5 text-xs font-bold text-orange-950 shadow-2xs"
        >
          <i class="mdi mdi-map-marker text-sm text-[#D96C2C]"></i>
          <span>{{ pinCount }} หมุดบนแผนที่</span>
        </span>
        <span
          v-if="missingCoordsCount"
          class="inline-flex items-center gap-1.5 rounded-full border border-[#E8D9C9] bg-white px-3.5 py-1.5 text-xs font-bold text-[#786B62]"
        >
          <i class="mdi mdi-map-marker-off-outline text-sm"></i>
          <span>{{ missingCoordsCount }} ร้านไม่มีพิกัด</span>
        </span>
      </div>
    </div>

    <!-- CONTENT -->
    <div class="relative grid grid-cols-1 lg:h-[620px] lg:grid-cols-12">
      <!-- SIDEBAR -->
      <div
        class="scrollbar-thin h-full max-h-[320px] space-y-3 overflow-y-auto border-b-2 border-[#E8D9C9] bg-[#F7F0E6] p-4 lg:col-span-4 lg:max-h-none lg:border-r-2 lg:border-b-0"
      >
        <div class="px-1 text-xs font-black tracking-wider text-[#D96C2C] uppercase">
          ร้านค้าทั้งหมด ({{ shops.length }})
        </div>

        <div v-if="loading" class="flex items-center justify-center py-12 text-sm text-[#786B62]">
          <i class="mdi mdi-loading mdi-spin mr-2"></i>
          กำลังโหลดร้านค้า...
        </div>

        <div
          v-else-if="!shops.length"
          class="py-12 text-center text-xs font-medium text-[#786B62]"
        >
          ไม่พบข้อมูลร้านค้าในระบบ
        </div>

        <template v-else>
          <div
            v-for="shop in shops"
            :id="`shop-map-item-${shop.shopId}`"
            :key="shop.shopId"
            :role="hasCoords(shop) ? 'button' : undefined"
            :tabindex="hasCoords(shop) ? 0 : undefined"
            :aria-disabled="!hasCoords(shop)"
            class="group flex h-[96px] w-full shrink-0 items-center gap-3.5 rounded-2xl border-2 bg-white p-3 transition-all duration-150 outline-none focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25"
            :class="[
              hasCoords(shop)
                ? 'cursor-pointer hover:-translate-y-0.5'
                : 'cursor-default opacity-75',
              selectedShopId === shop.shopId
                ? 'border-[#D96C2C] bg-orange-50/60 shadow-md ring-2 ring-[#D96C2C]/20'
                : 'border-[#E8D9C9] hover:border-[#D96C2C]/70 hover:shadow-sm',
            ]"
            @click="selectShop(shop)"
            @keydown.enter="selectShop(shop)"
          >
            <!-- รูป (ไม่มีรูปแสดงไอคอน ไม่ใช้รูปสุ่ม) -->
            <div
              class="relative h-14 w-14 shrink-0 overflow-hidden rounded-xl border border-[#E8D9C9] bg-[#FFF9F2]"
            >
              <img
                v-if="resolveImageUrl(shop.coverImageUrl)"
                :src="resolveImageUrl(shop.coverImageUrl)"
                :alt="shop.shopName"
                loading="lazy"
                decoding="async"
                width="56"
                height="56"
                class="h-full w-full object-cover transition-transform duration-200 group-hover:scale-105"
              />
              <div v-else class="flex h-full w-full items-center justify-center text-xl text-[#D96C2C]">
                <i class="mdi mdi-storefront-outline"></i>
              </div>
            </div>

            <div class="flex min-w-0 flex-1 flex-col justify-center space-y-0.5">
              <h4
                class="truncate text-xs leading-tight font-black text-[#332820] transition-colors group-hover:text-[#D96C2C] sm:text-sm"
              >
                {{ shop.shopName }}
              </h4>
              <p class="flex items-center gap-1 truncate text-[11px] font-semibold text-[#786B62]">
                <i class="mdi mdi-map-marker shrink-0 text-[#D96C2C]"></i>
                <span>อ.{{ shop.districtName || '-' }}</span>
                <span
                  v-if="!hasCoords(shop)"
                  class="ml-1 shrink-0 rounded-full bg-[#E8D9C9] px-2 py-px text-[10px] text-[#786B62]"
                >
                  ไม่มีพิกัด
                </span>
              </p>
              <div class="pt-0.5">
                <span
                  class="inline-block max-w-full truncate rounded-full border border-orange-200 bg-orange-50 px-2.5 py-0.5 text-[10px] font-bold text-orange-900"
                >
                  {{ shop.categoryName || 'ร้านค้าชุมชน' }}
                </span>
              </div>
            </div>

            <div
              v-if="hasCoords(shop)"
              class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full border border-[#E8D9C9] bg-[#FFF9F2] text-[#786B62] shadow-2xs transition group-hover:bg-[#D96C2C] group-hover:text-white"
            >
              <i class="mdi mdi-chevron-right text-base"></i>
            </div>
          </div>
        </template>
      </div>

      <!-- MAP -->
      <div class="relative h-[420px] lg:col-span-8 lg:h-full">
        <div ref="mapContainer" class="z-0 h-full w-full"></div>

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
              <span>กำลังโหลดหมุดร้านค้า...</span>
            </div>
          </div>
        </Transition>
      </div>
    </div>
  </div>
</template>

<style>
/* Popup */
.leaflet-popup-content-wrapper {
  padding: 0 !important;
  border-radius: 1.25rem !important;
  overflow: hidden !important;
  border: 2px solid #e8d9c9 !important;
  box-shadow:
    0 20px 25px -5px rgba(0, 0, 0, 0.15),
    0 8px 10px -6px rgba(0, 0, 0, 0.1) !important;
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
  color: #f2a65a !important;
}

.leaflet-container {
  font-family: inherit;
}

/* Marker wrappers must not draw Leaflet's default white box */
.shop-marker-wrapper {
  background: transparent !important;
  border: none !important;
}
.leaflet-marker-icon {
  will-change: transform;
}
.leaflet-map-pane {
  contain: layout style;
}

/* Warm, slightly muted basemap to match the cream/orange theme */
.leaflet-tile-pane {
  filter: saturate(0.8) sepia(0.12) brightness(1.02);
}

/* Cluster pill: district name + count */
.shop-cluster {
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
.shop-cluster:hover {
  transform: translate(-50%, -50%) scale(1.08);
}
.shop-cluster-count {
  min-width: 22px;
  padding: 3px 6px;
  border-radius: 9999px;
  background: #fff;
  color: #d96c2c;
  font-size: 12px;
  text-align: center;
}

/* Rounded zoom buttons */
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

/* Scrollbar */
.scrollbar-thin {
  scrollbar-width: thin;
  scrollbar-color: #d96c2c #f7f0e6;
}
.scrollbar-thin::-webkit-scrollbar {
  width: 6px;
}
.scrollbar-thin::-webkit-scrollbar-track {
  background: #f7f0e6;
}
.scrollbar-thin::-webkit-scrollbar-thumb {
  background: #d96c2c;
  border-radius: 999px;
}
</style>
