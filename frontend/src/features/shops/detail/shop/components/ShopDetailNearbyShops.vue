<script setup lang="ts">
import type { Shop } from '@/features/shops/api'

defineProps<{
  shop: Shop
  nearbyShops: Shop[]
  getNearbyShopImage: (target: Shop) => string
  formatShopArea: (target: Shop) => string
}>()
</script>

<template>
  <section
    v-if="nearbyShops.length"
    class="rounded-[1.75rem] bg-white border-2 border-[#E8D9C9] p-6 sm:p-7 shadow-sm space-y-6"
  >
    <div class="flex flex-wrap items-center justify-between gap-3 border-b-2 border-[#E8D9C9] pb-4">
      <div class="flex items-center gap-3">
        <div
          class="flex h-10 w-10 items-center justify-center rounded-full bg-[#FFF9F2] border-2 border-[#E8D9C9] text-[#D96C2C] shadow-xs font-bold"
        >
          <i class="mdi mdi-store-marker-outline text-xl text-[#D96C2C]"></i>
        </div>
        <div>
          <h2 class="text-xl font-black text-[#332820]">ร้านค้าใกล้เคียงในพื้นที่</h2>
          <p class="text-xs sm:text-sm text-[#786B62] font-semibold">
            ร้านค้าชุมชนอื่น ๆ ใน {{ shop.districtName ? `อ.${shop.districtName}` : 'กาญจนบุรี' }}
          </p>
        </div>
      </div>

      <RouterLink
        to="/shops"
        class="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-[#FFF9F2] border-2 border-[#E8D9C9] text-xs sm:text-sm font-bold text-[#D96C2C] hover:bg-[#D96C2C] hover:text-white hover:border-[#D96C2C] transition shadow-2xs"
      >
        <span>ดูร้านค้าทั้งหมด</span>
        <i class="mdi mdi-arrow-right text-sm"></i>
      </RouterLink>
    </div>

    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
      <RouterLink
        v-for="nb in nearbyShops"
        :key="nb.shopId"
        :to="`/shops/${nb.shopId}`"
        class="group bg-[#FFF9F2] rounded-[1.75rem] overflow-hidden border-2 border-[#E8D9C9] p-3.5 shadow-2xs hover:shadow-lg hover:border-[#D96C2C] hover:-translate-y-1 transition duration-300 flex flex-col justify-between"
      >
        <div>
          <div class="aspect-video rounded-2xl overflow-hidden bg-[#171412] mb-2.5">
            <img
              v-if="getNearbyShopImage(nb)"
              :src="getNearbyShopImage(nb)"
              :alt="nb.shopName"
              loading="lazy"
              class="w-full h-full object-cover group-hover:scale-105 transition duration-500"
            />
            <div v-else class="flex h-full w-full items-center justify-center bg-gradient-to-br from-[#D96C2C] via-[#BF5720] to-[#332820] text-white"><i class="mdi mdi-storefront-outline text-5xl opacity-40"></i></div>
          </div>
          <span
            class="text-[10px] font-black text-[#D96C2C] bg-white border border-[#E8D9C9] px-2.5 py-0.5 rounded-full inline-block mb-1 shadow-2xs"
          >
            {{ nb.categoryName || 'ร้านค้า' }}
          </span>
          <h4
            class="font-black text-[#332820] text-xs sm:text-sm group-hover:text-[#D96C2C] transition line-clamp-1"
          >
            {{ nb.shopName }}
          </h4>
          <p class="text-[11px] text-[#786B62] mt-0.5 font-semibold truncate">
            {{ formatShopArea(nb) }}
          </p>
        </div>

        <div
          class="flex items-center justify-between text-xs mt-3 pt-2 border-t border-[#E8D9C9] font-black text-[#D96C2C]"
        >
          <span>เข้าชมร้าน</span>
          <i class="mdi mdi-arrow-right group-hover:translate-x-1 transition"></i>
        </div>
      </RouterLink>
    </div>
  </section>
</template>
