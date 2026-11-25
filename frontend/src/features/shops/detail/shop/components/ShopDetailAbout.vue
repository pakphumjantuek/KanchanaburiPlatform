<script setup lang="ts">
import type { Shop } from '@/features/shops/api'

defineProps<{
  shop: Shop
}>()
</script>

<template>
  <section class="rounded-[1.75rem] bg-white border-2 border-[#E8D9C9] p-6 sm:p-7 shadow-sm space-y-5">
    <div class="flex items-center gap-3 border-b-2 border-[#E8D9C9] pb-4">
      <div class="flex h-10 w-10 items-center justify-center rounded-full bg-[#FFF9F2] border-2 border-[#E8D9C9] text-[#D96C2C] shadow-xs font-bold">
        <i class="mdi mdi-store-check-outline text-xl text-[#D96C2C]"></i>
      </div>
      <div>
        <h2 class="text-xl sm:text-2xl font-black text-[#332820]">เกี่ยวกับร้านค้า</h2>
        <p class="text-xs sm:text-sm text-[#786B62] font-semibold">ข้อมูลความเป็นมาและบริการของร้านค้า</p>
      </div>
    </div>

    <p
      v-if="shop.description"
      class="text-sm sm:text-base text-[#786B62] leading-relaxed whitespace-pre-line font-medium"
    >
      {{ shop.description }}
    </p>
    <p v-else class="text-sm font-medium text-[#A8988C]">ร้านค้ายังไม่ได้เพิ่มคำอธิบาย</p>
  </section>
</template>
