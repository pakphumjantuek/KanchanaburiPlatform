<script setup lang="ts">
import type { Shop } from '@/features/shops/api'

defineProps<{
  shop: Shop
  hasShopLocation: boolean
}>()

const emit = defineEmits<{
  (e: 'copy-link'): void
  (e: 'open-directions'): void
}>()
</script>

<template>
  <div
    class="sm:hidden fixed bottom-0 inset-x-0 z-30 bg-[#FFF9F2]/95 backdrop-blur-md border-t-2 border-[#E8D9C9] px-4 py-3 shadow-2xl flex items-center gap-2"
  >
    <button
      type="button"
      class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full border-2 border-[#E8D9C9] bg-white text-[#332820] cursor-pointer shadow-2xs"
      aria-label="แชร์ร้านค้านี้"
      @click="emit('copy-link')"
    >
      <i class="mdi mdi-share-variant-outline text-lg"></i>
    </button>

    <a
      v-if="shop.phone"
      :href="`tel:${shop.phone}`"
      class="flex-1 flex items-center justify-center gap-1.5 h-11 rounded-full bg-white border-2 border-[#E8D9C9] text-[#332820] font-black text-xs active:scale-95 transition shadow-2xs"
    >
      <i class="mdi mdi-phone-outline text-base text-[#D96C2C]"></i>
      <span>โทร</span>
    </a>

    <button
      v-if="hasShopLocation"
      type="button"
      class="flex-1 flex items-center justify-center gap-1.5 h-11 rounded-full bg-[#D96C2C] hover:bg-[#BF5720] text-white font-black text-xs shadow-sm active:scale-95 transition cursor-pointer border border-[#D96C2C]"
      @click="emit('open-directions')"
    >
      <i class="mdi mdi-navigation-variant text-base text-white"></i>
      <span class="text-white">นำทาง</span>
    </button>
  </div>
</template>
