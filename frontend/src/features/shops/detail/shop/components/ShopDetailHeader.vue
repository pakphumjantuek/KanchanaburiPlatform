<script setup lang="ts">
import { computed } from 'vue'
import { youtubeThumbnail } from '@/shared/utils/youtube'
import type { Shop } from '@/features/shops/api'
import type { PublicContent } from '@/features/contents/api'

const props = defineProps<{
  shop: Shop
  shopArea: string
  averageRating: number
  totalReviews: number
  totalProductsCount: number
  imageUrl: (url?: string) => string | undefined
  businessHours: string
  isOpenNow: boolean
  featuredContent?: PublicContent
}>()

const emit = defineEmits<{
  (e: 'copy-link'): void
}>()

// Use the shop's Background Banner (backgroundImageUrl) as requested by user
const bannerImageUrl = computed(() => {
  if (props.shop.backgroundImageUrl) {
    return props.imageUrl(props.shop.backgroundImageUrl)
  }
  if (props.shop.coverImageUrl) {
    return props.imageUrl(props.shop.coverImageUrl)
  }
  if (props.featuredContent?.youtubeUrl) {
    const thumb = youtubeThumbnail(props.featuredContent.youtubeUrl)
    if (thumb) return thumb
  }
  return undefined
})
</script>

<template>
  <div class="relative bg-[#F7F0E6] text-[#332820]">
    <!-- 1. Top Cover Banner Photo (Clear, Natural Colors without Dark Overlay) -->
    <div class="relative h-72 sm:h-96 md:h-[420px] w-full overflow-hidden bg-[#F7F0E6]">
      <img
        v-if="bannerImageUrl"
        :src="bannerImageUrl"
        :alt="shop.shopName"
        class="w-full h-full object-cover object-center transition duration-500"
      />
      <div v-else class="flex h-full w-full items-center justify-center bg-gradient-to-br from-[#D96C2C] via-[#BF5720] to-[#332820] text-white"><i class="mdi mdi-storefront-outline text-5xl opacity-40"></i></div>

      <!-- Top Tag Badge (Glassmorphism so readable on any background) -->
      <div
        v-if="shop.status === 'Active'"
        class="absolute top-4 left-4 sm:left-8 flex items-center gap-2 flex-wrap z-10"
      >
        <span
          class="px-4 py-1.5 rounded-full text-xs font-black bg-black/70 text-amber-300 backdrop-blur-md border border-white/20 shadow-lg flex items-center gap-1.5"
        >
          <i class="mdi mdi-shield-check text-amber-400 text-sm"></i>
          ร้านค้าที่ผ่านการรับรอง
        </span>
      </div>
    </div>

    <!-- 2. Overlapping Store Profile Card (Shifted Upwards) -->
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 -mt-12 sm:-mt-16 md:-mt-20 relative z-20">
      <div
        class="rounded-[1.75rem] bg-white p-6 sm:p-8 border-2 border-[#E8D9C9] shadow-lg space-y-6"
      >
        <div class="flex flex-col md:flex-row items-center md:items-start justify-between gap-6">
          <!-- Left: Avatar & Info -->
          <div
            class="flex flex-col sm:flex-row items-center sm:items-start gap-5 text-center sm:text-left"
          >
            <!-- Avatar Logo with Verified Ring -->
            <div class="relative shrink-0">
              <div
                v-if="!imageUrl(shop.coverImageUrl)"
                class="flex h-24 w-24 items-center justify-center rounded-[1.75rem] border-4 border-white bg-[#FFF9F2] text-4xl text-[#D96C2C] shadow-xl ring-4 ring-[#D96C2C]/20 sm:h-28 sm:w-28"
              >
                <i class="mdi mdi-storefront-outline"></i>
              </div>
              <img
                v-if="imageUrl(shop.coverImageUrl)"
                :src="imageUrl(shop.coverImageUrl)"
                :alt="shop.shopName"
                class="w-24 h-24 sm:w-28 sm:h-28 rounded-[1.75rem] object-cover border-4 border-white shadow-xl ring-4 ring-[#D96C2C]/20 bg-white"
              />
              <span
                v-if="shop.status === 'Active'"
                class="absolute -bottom-2 -right-2 flex h-8 w-8 items-center justify-center rounded-full bg-[#D96C2C] text-white shadow-md border-2 border-white"
                title="ร้านค้าได้รับการรับรอง"
              >
                <i class="mdi mdi-check-decagram text-base"></i>
              </span>
            </div>

            <!-- Identity Details -->
            <div class="space-y-2">
              <div class="flex flex-wrap items-center justify-center sm:justify-start! gap-2">
                <h1 class="text-2xl sm:text-3xl lg:text-4xl font-black text-[#332820] tracking-tight">
                  {{ shop.shopName }}
                </h1>
                <span
                  v-if="shop.status === 'Active'"
                  class="px-3.5 py-1 rounded-full text-xs font-black bg-[#D96C2C] text-white shadow-2xs"
                >
                  ผ่านการรับรอง
                </span>
              </div>

              <p
                v-if="shop.description"
                class="text-sm sm:text-base text-[#786B62] font-semibold max-w-xl line-clamp-2"
              >
                {{ shop.description }}
              </p>

              <!-- Rating, Products Count, and Location Bar (All Real Data) -->
              <div
                class="flex flex-wrap items-center justify-center sm:justify-start! gap-3 pt-1 text-xs sm:text-sm font-bold text-[#786B62]"
              >
                <div
                  v-if="totalReviews"
                  class="flex items-center gap-1 text-[#332820] bg-[#FFF9F2] px-3 py-1 rounded-full border border-[#E8D9C9] font-black shadow-2xs"
                >
                  <i class="mdi mdi-star text-base text-amber-500"></i>
                  <span>{{ averageRating.toFixed(1) }}</span>
                  <span class="text-[#786B62] font-semibold">({{ totalReviews }} รีวิว)</span>
                </div>
                <span v-else class="font-semibold text-[#786B62]">ยังไม่มีรีวิว</span>

                <span>•</span>
                <span class="text-[#332820] font-black flex items-center gap-1">
                  <i class="mdi mdi-package-variant text-base text-[#D96C2C]"></i>
                  มีสินค้า {{ totalProductsCount }} รายการ
                </span>

                <span>•</span>
                <span class="flex items-center gap-1 text-[#332820]">
                  <i class="mdi mdi-map-marker text-[#D96C2C] text-base"></i>
                  {{ shopArea }}
                </span>

                <template v-if="businessHours">
                  <span>•</span>
                  <span
                    class="px-3 py-1 rounded-full text-xs font-black"
                    :class="isOpenNow ? 'bg-emerald-100 text-emerald-800' : 'bg-rose-100 text-rose-800'"
                  >
                    {{ isOpenNow ? 'เปิดบริการอยู่' : 'ปิดทำการ' }} ({{ businessHours }})
                  </span>
                </template>
              </div>
            </div>
          </div>

          <!-- Right: Follow, Call & Share Buttons (Capsule Styling) -->
          <div
            class="grid grid-cols-12 gap-2 w-full sm:flex sm:w-auto sm:items-center sm:gap-3 shrink-0 pt-2 sm:pt-0"
          >
            <!-- Share Button -->
            <button
              type="button"
              class="col-span-2 sm:w-11 h-11 flex items-center justify-center rounded-full bg-[#FFF9F2] hover:bg-[#F7F0E6] text-[#332820] transition border-2 border-[#E8D9C9] shadow-xs cursor-pointer"
              title="คัดลอกลิงก์ร้านค้า"
              @click="emit('copy-link')"
            >
              <i class="mdi mdi-share-variant-outline text-lg text-[#332820]"></i>
            </button>

            <!-- Call Button -->
            <a
              v-if="shop.phone"
              :href="`tel:${shop.phone}`"
              class="col-span-10 sm:w-auto inline-flex items-center justify-center gap-1.5 px-4 sm:px-5 h-11 rounded-full border-2 border-[#E8D9C9] bg-[#FFF9F2] hover:bg-[#F7F0E6] text-[#332820] font-black text-xs sm:text-sm transition shadow-xs cursor-pointer"
            >
              <i class="mdi mdi-phone-outline text-base text-[#D96C2C]"></i>
              <span>โทรติดต่อ</span>
            </a>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
