<script setup lang="ts">
import { ref } from 'vue'
import type { Product } from '@/features/shops/api'
import { dealLabel, dealPrice } from '@/shared/utils/productDeal'

const imageFailed = ref(false)

defineProps<{
  product: Product
  imageUrl: (url?: string) => string | undefined
  formatPrice: (value: number) => string
  categoryName?: string
}>()
</script>

<template>
  <RouterLink
    :to="`/products/${product.productId}`"
    class="group flex flex-col justify-between overflow-hidden rounded-[1.5rem] border border-[#E8D9C9] bg-white shadow-xs transition-all duration-300 ease-out hover:-translate-y-1.5 hover:border-[#D96C2C] hover:shadow-2xl hover:shadow-[#D96C2C]/10 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
  >
    <div>
      <div class="relative aspect-[4/3] w-full overflow-hidden bg-[#F7F0E6]">
        <img
          v-if="imageUrl(product.imageUrl) && !imageFailed"
          :src="imageUrl(product.imageUrl)"
          :alt="product.productName"
          loading="lazy"
          class="h-full w-full object-cover transition-transform duration-700 ease-out group-hover:scale-105"
          :class="product.quantity <= 0 ? 'grayscale' : ''"
          @error="imageFailed = true"
        />
        <div v-else class="flex h-full w-full items-center justify-center text-[#E8D9C9]">
          <i class="mdi mdi-image-outline text-4xl"></i>
        </div>
        <div class="absolute inset-0 bg-gradient-to-t from-black/20 via-transparent to-transparent"></div>

        <span
          v-if="product.activeDeal"
          class="absolute top-2.5 left-2.5 rounded-full bg-rose-600 px-2.5 py-0.5 text-[11px] font-black text-white shadow-md"
        >
          {{ dealLabel(product) }}
        </span>
        <span
          v-if="product.quantity > 0 && product.quantity <= 5"
          class="absolute bottom-2.5 left-2.5 rounded-full border border-white/30 bg-slate-950/70 px-2.5 py-0.5 text-[11px] font-bold text-white backdrop-blur-md"
        >
          เหลือ {{ product.quantity }} ชิ้น
        </span>
        <div
          v-if="product.quantity <= 0"
          class="absolute inset-0 flex items-center justify-center bg-[#332820]/55"
        >
          <span class="rounded-full bg-white px-3.5 py-1 text-xs font-black text-[#332820] shadow-md">
            สินค้าหมด
          </span>
        </div>
      </div>

      <div class="space-y-1.5 p-3.5 sm:p-4">
        <span
          v-if="categoryName"
          class="inline-block max-w-full truncate rounded-full border border-[#E8D9C9] bg-[#FFF9F2] px-2.5 py-0.5 text-[11px] font-bold text-[#D96C2C]"
        >
          {{ categoryName }}
        </span>
        <h3
          class="line-clamp-2 text-sm leading-snug font-black text-[#332820] transition-colors group-hover:text-[#D96C2C] sm:text-base"
        >
          {{ product.productName }}
        </h3>
        <p
          v-if="product.description"
          class="line-clamp-2 text-xs leading-relaxed font-medium text-[#786B62]"
        >
          {{ product.description }}
        </p>
      </div>
    </div>

    <div
      class="flex items-center justify-between gap-2 border-t border-[#E8D9C9] bg-[#FFF9F2] px-3.5 py-3 sm:px-4"
    >
      <div class="flex min-w-0 flex-wrap items-baseline gap-x-2">
        <span class="text-base font-black text-[#D96C2C] sm:text-lg">{{
          formatPrice(dealPrice(product))
        }}</span>
        <span v-if="product.activeDeal" class="text-xs font-semibold text-[#786B62] line-through">
          {{ formatPrice(product.price) }}
        </span>
      </div>
      <span
        class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[#D96C2C] text-white shadow-md transition group-hover:bg-[#BF5720]"
        :class="product.quantity <= 0 ? 'bg-[#786B62]! group-hover:bg-[#786B62]!' : ''"
        :title="product.quantity <= 0 ? 'ดูรายละเอียด' : 'ดูสินค้า'"
      >
        <i
          class="mdi text-base text-white"
          :class="product.quantity <= 0 ? 'mdi-eye-outline' : 'mdi-arrow-right'"
        ></i>
      </span>
    </div>
  </RouterLink>
</template>
