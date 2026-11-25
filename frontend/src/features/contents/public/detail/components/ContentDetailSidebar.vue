<script setup lang="ts">
import { computed } from 'vue'
import type { PublicContent, ContentShopProduct } from '../../api/contentApi'
import type { Shop } from '@/features/shops/shared/types/shop'
import ContentLocationMap from './ContentLocationMap.vue'

interface Props {
  content: PublicContent
  shops?: Shop[]
  products?: ContentShopProduct[]
}

const props = defineProps<Props>()

const primaryShop = computed(() => (props.shops && props.shops.length > 0 ? props.shops[0] : null))

const emit = defineEmits<{
  'open-directions': []
}>()

const rawApiUrl = (import.meta.env.VITE_API_URL as string | undefined) ?? 'https://localhost:7289'
const apiOrigin = rawApiUrl.startsWith('http')
  ? rawApiUrl.replace(/\/api$/, '')
  : 'https://localhost:7289'

function resolveImageUrl(url?: string) {
  if (!url || !url.trim()) return ''
  if (url.startsWith('http://') || url.startsWith('https://')) return url
  return url.startsWith('/') ? `${apiOrigin}${url}` : `${apiOrigin}/${url}`
}

function getProductImage(url?: string) {
  const resolved = resolveImageUrl(url)
  if (resolved) return resolved
  return 'https://images.unsplash.com/photo-1544816155-12df9643f363?auto=format&fit=crop&w=300&q=80'
}

function getShopImage(url?: string) {
  const resolved = resolveImageUrl(url)
  if (resolved) return resolved
  return 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=300&q=80'
}

function formatPrice(value: number) {
  return new Intl.NumberFormat('th-TH', {
    style: 'currency',
    currency: 'THB',
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value)
}

const fallbackProductImg = 'https://images.unsplash.com/photo-1544816155-12df9643f363?auto=format&fit=crop&w=300&q=80'
const fallbackShopImg = 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=300&q=80'
</script>

<template>
  <div class="space-y-6">
    <!-- Card 1: เน้นร้านค้าและผลิตภัณฑ์ชุมชน (Primary Focus: Shops & Products) -->
    <div
      class="rounded-3xl bg-white border border-[#E8D9C9] p-5 sm:p-6 shadow-sm space-y-5 text-[#332820] relative overflow-hidden"
    >
      <div class="flex items-center justify-between border-b border-[#E8D9C9] pb-3.5">
        <div class="flex items-center gap-2.5">
          <div
            class="h-10 w-10 rounded-xl bg-[#D96C2C] text-white flex items-center justify-center font-bold shadow-xs shrink-0"
          >
            <i class="mdi mdi-storefront text-xl text-white"></i>
          </div>
          <div>
            <h3 class="font-black text-[#332820] text-base sm:text-lg">ร้านค้า & สินค้าชุมชน</h3>
            <p class="text-xs text-[#786B62] font-semibold">อุดหนุนและส่งเสริมเศรษฐกิจท้องถิ่น</p>
          </div>
        </div>
        <span
          class="inline-flex items-center gap-1 text-xs font-bold text-[#D96C2C] bg-[#FFF9F2] border border-[#E8D9C9] px-3 py-1 rounded-full shadow-2xs"
        >
          ร้านค้าชุมชน
        </span>
      </div>

      <!-- Shop Profile Banner (If Content belongs to or is linked to a shop) -->
      <div
        v-if="primaryShop"
        class="rounded-2xl border border-[#E8D9C9] bg-[#FFF9F2] p-4 space-y-3 shadow-2xs"
      >
        <div class="flex items-center gap-3">
          <div class="h-14 w-14 rounded-2xl overflow-hidden bg-slate-900 shrink-0 border border-[#E8D9C9] shadow-xs">
            <img
              :src="getShopImage(primaryShop.coverImageUrl)"
              :alt="primaryShop.shopName"
              class="h-full w-full object-cover"
              @error="(e) => (e.target as HTMLImageElement).src = fallbackShopImg"
            />
          </div>
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-1.5 flex-wrap">
              <span class="font-black text-[#332820] text-base leading-snug truncate">
                {{ primaryShop.shopName }}
              </span>
            </div>
            <p class="text-xs text-[#786B62] font-medium flex items-center gap-1 mt-0.5">
              <i class="mdi mdi-map-marker text-[#D96C2C]"></i>
              <span>อ.{{ primaryShop.districtName || content.districtName || 'กาญจนบุรี' }}</span>
            </p>
          </div>
        </div>

        <RouterLink
          :to="`/shops/${primaryShop.shopId}`"
          class="w-full py-2.5 rounded-full bg-[#D96C2C] hover:bg-[#BF5720] text-white text-xs font-bold transition flex items-center justify-center gap-1.5 shadow-sm active:scale-95 cursor-pointer"
        >
          <i class="mdi mdi-store-outline text-sm"></i>
          <span>เข้าชมหน้าร้านค้านี้</span>
          <i class="mdi mdi-arrow-right text-xs"></i>
        </RouterLink>
      </div>

      <!-- Products Highlight List -->
      <div v-if="products && products.length" class="space-y-3">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold text-[#D96C2C] uppercase tracking-wide flex items-center gap-1">
            <i class="mdi mdi-tag-outline text-[#D96C2C]"></i>
            สินค้าแนะนำในสถานที่นี้
          </span>
          <span class="text-xs font-bold text-[#786B62]">({{ products.length }} รายการ)</span>
        </div>

        <div class="space-y-2.5">
          <RouterLink
            v-for="prod in products.slice(0, 4)"
            :key="prod.productId"
            :to="`/products/${prod.productId}`"
            class="group flex items-center gap-3 p-2.5 rounded-2xl border border-[#E8D9C9] bg-[#FFF9F2] hover:border-[#D96C2C] transition cursor-pointer shadow-2xs"
          >
            <!-- Product Thumbnail Image -->
            <div class="h-14 w-14 rounded-xl overflow-hidden bg-slate-900 shrink-0 border border-[#E8D9C9] relative shadow-2xs">
              <img
                :src="getProductImage(prod.imageUrl)"
                :alt="prod.productName"
                class="h-full w-full object-cover group-hover:scale-110 transition duration-300"
                @error="(e) => (e.target as HTMLImageElement).src = fallbackProductImg"
              />
            </div>

            <!-- Product Title & Price -->
            <div class="flex-1 min-w-0">
              <span class="font-black text-[#332820] text-sm block truncate group-hover:text-[#D96C2C] transition">
                {{ prod.productName }}
              </span>
              <p class="text-xs text-[#786B62] line-clamp-1 mt-0.5 font-medium">
                {{ prod.description || 'สินค้าภูมิปัญญาท้องถิ่น' }}
              </p>
              <span class="inline-block mt-1 font-bold text-[#D96C2C] text-xs bg-orange-100/70 px-2.5 py-0.5 rounded-full border border-orange-200">
                {{ formatPrice(prod.price) }}
              </span>
            </div>
          </RouterLink>
        </div>
      </div>

      <!-- Additional Recommended Shops if more than 1 -->
      <div v-if="shops && shops.length > 1" class="pt-3 border-t-2 border-[#E8D9C9] space-y-2.5">
        <span class="text-xs font-bold text-[#786B62] uppercase tracking-wide block">
          ร้านค้าอื่น ๆ ในบริเวณใกล้เคียง
        </span>
        <div class="space-y-2">
          <RouterLink
            v-for="shop in shops.slice(1, 3)"
            :key="shop.shopId"
            :to="`/shops/${shop.shopId}`"
            class="group flex items-center gap-3 p-2.5 rounded-2xl border border-[#E8D9C9] bg-[#FFF9F2] hover:border-[#D96C2C] transition shadow-2xs"
          >
            <div class="h-11 w-11 rounded-xl overflow-hidden bg-slate-900 shrink-0 border border-[#E8D9C9]">
              <img
                :src="getShopImage(shop.coverImageUrl)"
                :alt="shop.shopName"
                class="h-full w-full object-cover"
                @error="(e) => (e.target as HTMLImageElement).src = fallbackShopImg"
              />
            </div>
            <div class="flex-1 min-w-0">
              <span class="font-bold text-[#332820] text-xs sm:text-sm block truncate group-hover:text-[#D96C2C] transition">
                {{ shop.shopName }}
              </span>
              <span class="text-xs text-[#786B62] block truncate">
                อ.{{ shop.districtName || 'กาญจนบุรี' }}
              </span>
            </div>
            <i class="mdi mdi-chevron-right text-[#786B62] group-hover:text-[#D96C2C]"></i>
          </RouterLink>
        </div>
      </div>

      <!-- All Shops Link Button -->
      <div class="pt-1">
        <RouterLink
          to="/shops"
          class="w-full py-2.5 rounded-full bg-[#FFF9F2] hover:bg-[#F7F0E6] text-[#D96C2C] border border-[#E8D9C9] hover:border-[#D96C2C] font-bold text-xs sm:text-sm transition flex items-center justify-center gap-1.5 shadow-2xs active:scale-95 select-none"
        >
          <span>สำรวจร้านค้า & สินค้าชุมชนทั้งหมด</span>
          <i class="mdi mdi-arrow-right text-xs"></i>
        </RouterLink>
      </div>
    </div>

    <!-- Card 2: พิกัดสถานที่และแผนที่นำทาง (Location & Map) -->
    <div
      class="rounded-3xl bg-white border border-[#E8D9C9] p-5 sm:p-6 shadow-sm space-y-4 text-[#332820]"
    >
      <div class="flex items-center justify-between border-b border-[#E8D9C9] pb-3">
        <div class="flex items-center gap-2.5">
          <div
            class="h-9 w-9 rounded-xl bg-[#FFF1E8] text-[#D96C2C] flex items-center justify-center shadow-xs"
          >
            <i class="mdi mdi-map-marker text-lg text-[#D96C2C]"></i>
          </div>
          <div>
            <h3 class="font-black text-[#332820] text-base sm:text-lg">พิกัดและเส้นทาง</h3>
            <p class="text-xs text-[#786B62] font-semibold">แผนที่ท่องเที่ยวจังหวัดกาญจนบุรี</p>
          </div>
        </div>
        <span
          class="inline-flex items-center gap-1.5 text-xs font-bold text-[#D96C2C] bg-[#FFF9F2] border border-[#E8D9C9] px-3 py-1 rounded-full shadow-2xs"
        >
          <span class="h-2 w-2 rounded-full bg-[#D96C2C] animate-pulse"></span>
          พร้อมนำทาง
        </span>
      </div>

      <div class="space-y-1">
        <h4 class="font-black text-[#332820] text-base leading-snug">{{ content.title }}</h4>
        <p class="text-xs sm:text-sm text-[#786B62] font-semibold flex items-center gap-1">
          <i class="mdi mdi-map-marker-outline text-[#D96C2C]"></i>
          <span>
            {{
              [content.subDistrictName, content.districtName].filter(Boolean).join(' ') ||
              'สังขละบุรี กาญจนบุรี'
            }}
          </span>
        </p>
      </div>

      <!-- Embedded Location Map -->
      <div class="rounded-2xl overflow-hidden border border-[#E8D9C9] shadow-inner min-h-[190px]">
        <ContentLocationMap
          v-if="content.latitude && content.longitude"
          :latitude="content.latitude"
          :longitude="content.longitude"
          :title="content.title"
        />
        <div
          v-else
          class="h-40 bg-[#F7F0E6] flex flex-col items-center justify-center gap-2 text-[#786B62] text-xs sm:text-sm font-semibold"
        >
          <i class="mdi mdi-map-marker-off text-3xl text-[#D96C2C]"></i>
          <span>ยังไม่มีพิกัดในระบบ</span>
        </div>
      </div>

      <div class="space-y-3 pt-1">
        <span
          v-if="content.latitude && content.longitude"
          class="text-xs font-bold text-[#786B62] flex items-center gap-1.5 bg-[#FFF9F2] border border-[#E8D9C9] px-3 py-1.5 rounded-full w-full justify-center shadow-2xs"
        >
          <i class="mdi mdi-crosshairs-gps text-[#D96C2C]"></i>
          GPS: {{ content.latitude.toFixed(4) }}, {{ content.longitude.toFixed(4) }}
        </span>

        <!-- Navigation Button (Capsule Pill) -->
        <button
          type="button"
          class="w-full py-3 rounded-full bg-[#D96C2C] hover:bg-[#BF5720] text-white text-xs sm:text-sm font-bold transition flex items-center justify-center gap-2 shadow-md active:scale-95 cursor-pointer border border-[#D96C2C] select-none outline-none focus:outline-none"
          @click="emit('open-directions')"
        >
          <i class="mdi mdi-navigation-variant text-base text-white"></i>
          <span class="text-white">เปิดแผนที่นำทาง (Google Maps)</span>
        </button>
      </div>
    </div>
  </div>
</template>
