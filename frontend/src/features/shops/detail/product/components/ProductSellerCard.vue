<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import type { Shop } from '@/features/shops/api'

const props = defineProps<{
  shop: Shop
  imageUrl: (url?: string) => string | undefined
}>()

const emit = defineEmits<{
  (e: 'open-directions'): void
}>()

// shop names are merchant-authored and end up in the map popup HTML
function esc(value: unknown): string {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')
}

function hhmm(value?: string) {
  if (!value) return ''
  const m = /^(\d{1,2}):(\d{2})/.exec(value.trim())
  return m ? `${m[1]!.padStart(2, '0')}:${m[2]}` : value.trim()
}

const hoursText = computed(() => {
  const open = hhmm(props.shop.openingTime)
  const close = hhmm(props.shop.closingTime)
  return open && close ? `${open} - ${close} น.` : ''
})

const hasContact = computed(() => Boolean(props.shop.phone || hoursText.value || props.shop.address))

const mapContainer = ref<HTMLElement | null>(null)
let mapInstance: L.Map | null = null

function initMap() {
  if (!mapContainer.value || mapInstance) return
  if (props.shop.latitude == null || props.shop.longitude == null) return

  const lat = props.shop.latitude
  const lng = props.shop.longitude

  const KANCHANABURI_BOUNDS: L.LatLngBoundsExpression = [
    [13.7, 98.0],
    [15.85, 100.0],
  ]

  mapInstance = L.map(mapContainer.value, {
    scrollWheelZoom: false,
    zoomControl: false,
    minZoom: 9,
    maxZoom: 18,
    maxBounds: KANCHANABURI_BOUNDS,
    maxBoundsViscosity: 0.4,
  }).setView([lat, lng], 14)

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 18,
    attribution: '&copy; OpenStreetMap',
    bounds: KANCHANABURI_BOUNDS,
    keepBuffer: 6,
  }).addTo(mapInstance)

  const icon = L.divIcon({
    className: 'custom-product-shop-marker',
    html: `<div class="flex items-center justify-center h-10 w-10 rounded-full bg-[#D96C2C] text-white font-bold shadow-2xl border-2 border-white"><i class="mdi mdi-storefront text-xl text-white"></i></div>`,
    iconSize: [40, 40],
    iconAnchor: [20, 20],
  })

  L.marker([lat, lng], { icon })
    .addTo(mapInstance)
    .bindPopup(`<b class="text-[#332820] font-black">${esc(props.shop.shopName || 'ร้านค้าชุมชน')}</b>`)
}

onMounted(() => {
  setTimeout(() => initMap(), 350)
})

onBeforeUnmount(() => {
  if (mapInstance) {
    mapInstance.remove()
    mapInstance = null
  }
})
</script>

<template>
  <div class="space-y-6 lg:sticky lg:top-28">
    <!-- CARD 1: seller -->
    <div
      class="space-y-4 rounded-3xl border border-[#E8D9C9] bg-white p-6 shadow-md shadow-[#D96C2C]/5"
    >
      <h3
        class="flex items-center gap-2 border-b border-[#E8D9C9] pb-3 text-base font-black text-[#332820]"
      >
        <i class="mdi mdi-storefront-outline text-lg text-[#D96C2C]"></i>
        <span>ร้านผู้จำหน่าย</span>
      </h3>

      <div class="flex items-center gap-3.5">
        <img
          v-if="imageUrl(shop.coverImageUrl)"
          :src="imageUrl(shop.coverImageUrl)"
          :alt="shop.shopName"
          class="h-14 w-14 shrink-0 rounded-2xl border-2 border-white object-cover shadow-md ring-2 ring-[#D96C2C]/20"
        />
        <div
          v-else
          class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl border-2 border-white bg-[#FFF9F2] text-2xl text-[#D96C2C] shadow-md ring-2 ring-[#D96C2C]/20"
        >
          <i class="mdi mdi-storefront-outline"></i>
        </div>
        <div class="min-w-0">
          <h4 class="flex items-center gap-1 text-sm font-black text-[#332820]">
            <span class="truncate">{{ shop.shopName }}</span>
            <i
              v-if="shop.status === 'Active'"
              class="mdi mdi-check-decagram shrink-0 text-sm text-[#D96C2C]"
              title="ร้านค้าที่ผ่านการรับรอง"
            ></i>
          </h4>
          <p class="text-xs font-semibold text-[#786B62]">
            {{ shop.categoryName || 'ร้านค้าชุมชน'
            }}<template v-if="shop.districtName"> • อ.{{ shop.districtName }}</template>
          </p>
        </div>
      </div>

      <dl v-if="hasContact" class="space-y-2.5 pt-1 text-sm font-bold text-[#332820]">
        <div v-if="shop.phone" class="flex items-center gap-2.5">
          <i class="mdi mdi-phone-outline shrink-0 text-base text-[#D96C2C]"></i>
          <a :href="`tel:${shop.phone}`" class="transition hover:text-[#D96C2C] hover:underline">{{
            shop.phone
          }}</a>
        </div>
        <div v-if="hoursText" class="flex items-center gap-2.5">
          <i class="mdi mdi-clock-outline shrink-0 text-base text-[#D96C2C]"></i>
          <span>เปิด {{ hoursText }}</span>
        </div>
        <div v-if="shop.address" class="flex items-start gap-2.5">
          <i class="mdi mdi-map-marker-outline mt-0.5 shrink-0 text-base text-[#D96C2C]"></i>
          <span>{{ shop.address }}</span>
        </div>
      </dl>
      <p v-else class="text-xs font-medium text-[#A8988C]">ร้านค้ายังไม่ได้ระบุข้อมูลติดต่อ</p>

      <RouterLink
        :to="`/shops/${shop.shopId}`"
        class="flex w-full items-center justify-center gap-1.5 rounded-full bg-[#D96C2C] py-3 text-center text-sm font-black text-white shadow-md shadow-[#D96C2C]/30 transition hover:bg-[#BF5720] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95"
      >
        <span class="text-white">เข้าชมร้านค้า</span>
        <i class="mdi mdi-arrow-right text-base text-white"></i>
      </RouterLink>
    </div>

    <!-- CARD 2: ตำแหน่งร้าน (SHOP LOCATION MAP CARD) -->
    <div class="space-y-3 rounded-3xl border border-[#E8D9C9] bg-white p-6 shadow-md shadow-[#D96C2C]/5">
      <h3 class="font-black text-[#332820] text-base flex items-center gap-2">
        <i class="mdi mdi-map-marker text-[#D96C2C] text-lg"></i>
        <span>ตำแหน่งร้านค้า</span>
      </h3>

      <div
        v-if="shop.latitude != null && shop.longitude != null"
        class="rounded-2xl overflow-hidden border border-[#E8D9C9] h-48"
      >
        <div ref="mapContainer" class="w-full h-full bg-[#F7F0E6]"></div>
      </div>
      <div
        v-else
        class="flex h-44 items-center justify-center rounded-2xl border-2 border-dashed border-[#E8D9C9] bg-[#F7F0E6] text-xs text-[#786B62]"
      >
        ร้านค้ายังไม่ได้ระบุตำแหน่งบนแผนที่
      </div>

      <button
        v-if="shop.latitude != null && shop.longitude != null"
        type="button"
        class="w-full py-2.5 rounded-xl border border-[#E8D9C9] text-center font-black text-xs text-[#332820] hover:bg-[#F7F0E6] transition flex items-center justify-center gap-1 cursor-pointer"
        @click="emit('open-directions')"
      >
        <i class="mdi mdi-map-marker-outline text-[#D96C2C]"></i>
        <span>ดูแผนที่ขนาดใหญ่</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
:deep(.custom-product-shop-marker) {
  background: transparent;
  border: none;
}
</style>
