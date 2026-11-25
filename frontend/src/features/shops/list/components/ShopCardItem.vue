<script setup lang="ts">
import { computed } from 'vue'
import type { Product, Shop } from '@/features/shops/api'

const props = defineProps<{
  shop: Shop
  products?: Product[]
}>()

const apiOrigin = (import.meta.env.VITE_API_URL ?? 'https://localhost:7289/api').replace(
  /\/api$/,
  '',
)

function imageUrl(url?: string) {
  if (!url) return undefined
  return url.startsWith('/') ? `${apiOrigin}${url}` : url
}

const bannerUrl = computed(() => imageUrl(props.shop.backgroundImageUrl || props.shop.coverImageUrl))
const logoUrl = computed(() => imageUrl(props.shop.coverImageUrl))

// Show the times as stored; trim "08:00:00" to "08:00" when it is in that shape
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

// Only the real status from the API drives the badge (shown for active shops)
const isActive = computed(() => props.shop.status === 'Active')
</script>

<template>
  <div
    class="group flex flex-col justify-between overflow-hidden rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white shadow-xs transition-all duration-300 ease-out hover:-translate-y-1.5 hover:border-[#D96C2C] hover:shadow-2xl hover:shadow-[#D96C2C]/10"
  >
    <div>
      <!-- Cover banner -->
      <RouterLink
        :to="`/shops/${shop.shopId}`"
        class="relative block aspect-[16/9] w-full overflow-hidden bg-[#171412] focus-visible:ring-4 focus-visible:ring-inset focus-visible:ring-[#D96C2C]/40 focus-visible:outline-none"
        :aria-label="`ดูร้าน ${shop.shopName}`"
      >
        <img
          v-if="bannerUrl"
          :src="bannerUrl"
          :alt="shop.shopName"
          loading="lazy"
          class="h-full w-full object-cover transition-transform duration-700 ease-out group-hover:scale-105"
        />
        <div
          v-else
          class="flex h-full w-full items-center justify-center bg-gradient-to-br from-[#D96C2C] via-[#BF5720] to-[#332820] text-white"
        >
          <i class="mdi mdi-storefront-outline text-5xl opacity-40"></i>
        </div>
        <div class="absolute inset-0 bg-gradient-to-t from-black/60 via-black/0 to-black/20"></div>

        <span
          v-if="isActive"
          class="absolute top-3 left-3 flex items-center gap-1.5 rounded-full border border-white/20 bg-[#D96C2C]/95 px-3 py-1 text-xs font-black text-white shadow-md backdrop-blur-md"
        >
          <i class="mdi mdi-shield-check text-xs"></i>
          <span>ผ่านการรับรอง</span>
        </span>

        <span
          v-if="shop.districtName"
          class="absolute top-3 right-3 flex items-center gap-1 rounded-full border border-white/20 bg-slate-900/85 px-3 py-1 text-xs font-bold text-white shadow-md backdrop-blur-md"
        >
          <i class="mdi mdi-map-marker text-xs text-[#F2A65A]"></i>
          <span>อ.{{ shop.districtName }}</span>
        </span>
      </RouterLink>

      <!-- Shop identity -->
      <div class="space-y-4 p-5">
        <div class="flex items-center gap-3.5">
          <img
            v-if="logoUrl"
            :src="logoUrl"
            :alt="shop.shopName"
            loading="lazy"
            class="h-14 w-14 shrink-0 rounded-2xl border-2 border-white object-cover shadow-md ring-2 ring-[#D96C2C]/20"
          />
          <div
            v-else
            class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl border-2 border-white bg-[#FFF9F2] text-2xl text-[#D96C2C] shadow-md ring-2 ring-[#D96C2C]/20"
          >
            <i class="mdi mdi-storefront-outline"></i>
          </div>
          <div class="min-w-0 flex-1">
            <h3
              class="truncate text-base font-black text-[#332820] transition-colors group-hover:text-[#D96C2C] sm:text-lg"
            >
              <RouterLink :to="`/shops/${shop.shopId}`" class="focus-visible:underline focus-visible:outline-none">{{ shop.shopName }}</RouterLink>
            </h3>
            <p class="mt-0.5 truncate text-xs font-semibold text-[#786B62]">
              {{ shop.categoryName || 'ร้านค้าชุมชน' }}
              <template v-if="shop.subDistrictName"> • ต.{{ shop.subDistrictName }}</template>
            </p>
          </div>
        </div>

        <!-- Description & opening hours -->
        <div v-if="shop.description || hoursText" class="space-y-1.5">
          <p
            v-if="shop.description"
            class="line-clamp-2 text-xs leading-relaxed font-medium text-[#786B62]"
          >
            {{ shop.description }}
          </p>
          <p v-if="hoursText" class="flex items-center gap-1.5 text-xs font-bold text-[#332820]">
            <i class="mdi mdi-clock-outline text-sm text-[#D96C2C]"></i>
            เปิด {{ hoursText }}
          </p>
        </div>

        <!-- Product count -->
        <div
          class="flex items-center justify-between rounded-2xl border border-[#E8D9C9] bg-[#FFF9F2] px-3.5 py-2.5 text-xs font-bold"
        >
          <span class="text-[#786B62]">สินค้าในร้าน</span>
          <span class="flex items-center gap-1 font-black text-[#D96C2C]">
            <i class="mdi mdi-package-variant"></i>
            {{ products?.length || 0 }} รายการ
          </span>
        </div>
      </div>
    </div>

    <!-- Action -->
    <div class="p-5 pt-0">
      <RouterLink
        :to="`/shops/${shop.shopId}`"
        class="flex w-full cursor-pointer items-center justify-center gap-1.5 rounded-full bg-[#D96C2C] px-4 py-3 text-xs font-black text-white shadow-sm transition hover:bg-[#BF5720] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95 sm:text-sm"
      >
        <span class="!text-white">เข้าชมหน้าร้าน</span>
        <i class="mdi mdi-arrow-right text-sm text-white transition-transform group-hover:translate-x-1"></i>
      </RouterLink>
    </div>
  </div>
</template>
