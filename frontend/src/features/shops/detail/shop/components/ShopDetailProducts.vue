<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Product, ProductCategory, Shop } from '@/features/shops/api'
import { dealLabel, dealPrice } from '@/shared/utils/productDeal'
import { vReveal } from '@/shared/directives/reveal'

const props = defineProps<{
  shop: Shop
  products: Product[]
  categories: ProductCategory[]
  filteredProducts: Product[]
  productCategoryName: (id: string) => string
  imageUrl: (url?: string) => string | undefined
  formatPrice: (value: number) => string
  getProductImage: (prod: Product, index: number) => string
}>()

const selectedCategory = defineModel<string | null>('selectedCategory', { default: null })
const productSearchInput = defineModel<string>('productSearchInput', { default: '' })
const productSearch = defineModel<string>('productSearch', { default: '' })

// Only sorts the data can actually support (there is no sales / popularity data)
type SortKey = 'default' | 'price-asc' | 'price-desc'
const sortBy = ref<SortKey>('default')
const sortChoices: { value: SortKey; label: string; icon: string }[] = [
  { value: 'default', label: 'แนะนำ', icon: 'mdi-star-four-points-outline' },
  { value: 'price-asc', label: 'ราคาต่ำ-สูง', icon: 'mdi-sort-numeric-ascending' },
  { value: 'price-desc', label: 'ราคาสูง-ต่ำ', icon: 'mdi-sort-numeric-descending' },
]

const MAX_SHOWN = 8

const productCategories = computed(() => [
  { id: null, name: 'ทั้งหมด', count: props.products.length },
  ...props.categories
    .filter((category) =>
      props.products.some((product) => product.productCategoryId === category.productCategoryId),
    )
    .map((category) => ({
      id: category.productCategoryId,
      name: category.categoryName,
      count: props.products.filter((p) => p.productCategoryId === category.productCategoryId)
        .length,
    })),
])

const sortedFilteredProducts = computed(() => {
  const list = [...props.filteredProducts]
  // sort on the price the buyer will actually pay (deal price when there is a deal)
  if (sortBy.value === 'price-asc') list.sort((a, b) => dealPrice(a) - dealPrice(b))
  else if (sortBy.value === 'price-desc') list.sort((a, b) => dealPrice(b) - dealPrice(a))
  return list
})

const shownProducts = computed(() => sortedFilteredProducts.value.slice(0, MAX_SHOWN))

function clearSearch() {
  productSearchInput.value = ''
  productSearch.value = ''
}

function clearFilters() {
  clearSearch()
  selectedCategory.value = null
}
</script>

<template>
  <section
    class="space-y-6 rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white p-6 shadow-md shadow-[#D96C2C]/5 sm:p-8"
  >
    <!-- Header: title + search -->
    <div
      class="flex flex-col justify-between gap-4 border-b-2 border-[#E8D9C9] pb-5 md:flex-row md:items-center"
    >
      <div class="flex items-center gap-3.5">
        <div
          class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-[#D96C2C] text-white shadow-md"
        >
          <i class="mdi mdi-storefront-outline text-2xl"></i>
        </div>
        <div>
          <h2 class="text-xl font-black tracking-tight text-[#332820] sm:text-2xl">สินค้าในร้าน</h2>
          <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
            แสดง {{ shownProducts.length }} จาก {{ products.length }} รายการ
          </p>
        </div>
      </div>

      <div
        class="flex w-full items-center gap-1.5 rounded-2xl border-2 border-[#E8D9C9] bg-white p-1.5 shadow-sm transition focus-within:border-[#D96C2C] focus-within:shadow-md focus-within:ring-4 focus-within:ring-[#D96C2C]/15 md:w-80"
      >
        <span
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#FFF9F2] text-lg text-[#D96C2C]"
        >
          <i class="mdi mdi-magnify"></i>
        </span>
        <input
          v-model="productSearchInput"
          type="search"
          autocomplete="off"
          placeholder="ค้นหาสินค้าในร้านนี้..."
          aria-label="ค้นหาสินค้าในร้านนี้"
          class="min-w-0 flex-1 bg-transparent py-2 text-sm font-bold text-[#332820] outline-none placeholder:font-medium placeholder:text-[#A8988C] [&::-webkit-search-cancel-button]:hidden"
        />
        <button
          v-if="productSearchInput"
          type="button"
          class="mr-1 flex h-7 w-7 shrink-0 cursor-pointer items-center justify-center rounded-full text-[#786B62] transition hover:bg-[#F7F0E6] hover:text-[#332820]"
          aria-label="ล้างคำค้นหา"
          @click="clearSearch"
        >
          <i class="mdi mdi-close text-base"></i>
        </button>
      </div>
    </div>

    <!-- Filters: categories (left) + sort (right) -->
    <div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
      <div
        v-if="productCategories.length > 1"
        class="scrollbar-none flex items-center gap-2 overflow-x-auto pb-1"
      >
        <button
          v-for="cat in productCategories"
          :key="cat.name"
          type="button"
          class="flex shrink-0 cursor-pointer items-center gap-1.5 rounded-full border-2 px-4 py-2 text-xs font-black shadow-2xs transition select-none focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none active:scale-95 sm:text-sm"
          :class="
            selectedCategory === cat.id
              ? 'border-[#D96C2C] bg-[#D96C2C] text-white shadow-sm'
              : 'border-[#E8D9C9] bg-[#FFF9F2] text-[#332820] hover:border-[#D96C2C]'
          "
          @click="selectedCategory = cat.id"
        >
          <span>{{ cat.name }}</span>
          <span
            class="rounded-full px-2 py-0.5 text-xs"
            :class="
              selectedCategory === cat.id
                ? 'bg-white font-black text-[#D96C2C]'
                : 'bg-[#E8D9C9]/60 font-extrabold text-[#786B62]'
            "
          >
            {{ cat.count }}
          </span>
        </button>
      </div>

      <div
        class="flex shrink-0 flex-wrap items-center gap-1.5"
        role="radiogroup"
        aria-label="เรียงลำดับสินค้า"
      >
        <span class="mr-0.5 hidden text-xs font-bold text-[#786B62] sm:inline">เรียง:</span>
        <button
          v-for="opt in sortChoices"
          :key="opt.value"
          type="button"
          role="radio"
          :aria-checked="sortBy === opt.value"
          class="flex shrink-0 cursor-pointer items-center gap-1 rounded-full border-2 px-3 py-1.5 text-xs font-black whitespace-nowrap transition select-none focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none active:scale-95"
          :class="
            sortBy === opt.value
              ? 'border-[#D96C2C] bg-[#FFF9F2] text-[#D96C2C]'
              : 'border-[#E8D9C9] bg-white text-[#786B62] hover:border-[#D96C2C] hover:text-[#332820]'
          "
          @click="sortBy = opt.value"
        >
          <i class="mdi text-sm" :class="opt.icon"></i>
          {{ opt.label }}
        </button>
      </div>
    </div>

    <!-- Products grid -->
    <div
      v-if="shownProducts.length"
      class="grid grid-cols-2 gap-4 sm:grid-cols-3 sm:gap-5 lg:grid-cols-4"
    >
      <RouterLink
        v-for="(prod, idx) in shownProducts"
        :key="prod.productId"
        v-reveal
        :to="`/products/${prod.productId}`"
        :style="{ animationDelay: `${(idx % 4) * 80}ms` }"
        class="group flex flex-col justify-between overflow-hidden rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white shadow-xs transition-all duration-300 ease-out hover:-translate-y-1.5 hover:border-[#D96C2C] hover:shadow-2xl hover:shadow-[#D96C2C]/10 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
      >
        <div>
          <!-- Photo -->
          <div class="relative aspect-[4/3] w-full overflow-hidden bg-[#F7F0E6]">
            <img
              v-if="getProductImage(prod, idx)"
              :src="getProductImage(prod, idx)"
              :alt="prod.productName"
              loading="lazy"
              class="h-full w-full object-cover object-center transition-transform duration-700 ease-out group-hover:scale-105"
              :class="prod.quantity <= 0 ? 'grayscale' : ''"
            />
            <div v-else class="flex h-full w-full items-center justify-center text-[#E8D9C9]">
              <i class="mdi mdi-image-outline text-5xl"></i>
            </div>
            <div class="absolute inset-0 bg-gradient-to-t from-black/25 via-transparent to-transparent"></div>

            <span
              v-if="prod.activeDeal"
              class="absolute top-2.5 left-2.5 rounded-full bg-rose-600 px-2.5 py-1 text-[11px] font-black text-white shadow-md"
            >
              {{ dealLabel(prod) }}
            </span>

            <!-- Real stock info -->
            <span
              v-if="prod.quantity > 0 && prod.quantity <= 5"
              class="absolute bottom-2.5 left-2.5 rounded-full border border-white/30 bg-slate-950/70 px-2.5 py-1 text-[11px] font-bold text-white backdrop-blur-md"
            >
              เหลือ {{ prod.quantity }} ชิ้น
            </span>
            <div
              v-if="prod.quantity <= 0"
              class="absolute inset-0 flex items-center justify-center bg-[#332820]/55"
            >
              <span class="rounded-full bg-white px-4 py-1.5 text-xs font-black text-[#332820] shadow-md">
                สินค้าหมด
              </span>
            </div>
          </div>

          <!-- Info -->
          <div class="space-y-2 p-4">
            <span
              class="inline-block max-w-full truncate rounded-full border border-[#E8D9C9] bg-[#FFF9F2] px-2.5 py-0.5 text-[11px] font-bold text-[#D96C2C]"
            >
              {{ productCategoryName(prod.productCategoryId) }}
            </span>
            <h3
              class="line-clamp-2 text-sm leading-snug font-black text-[#332820] transition-colors group-hover:text-[#D96C2C] sm:text-base"
            >
              {{ prod.productName }}
            </h3>
            <div class="flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
              <span class="text-lg font-black text-[#D96C2C] sm:text-xl">{{
                formatPrice(dealPrice(prod))
              }}</span>
              <span v-if="prod.activeDeal" class="text-xs font-semibold text-[#786B62] line-through">
                {{ formatPrice(prod.price) }}
              </span>
            </div>
          </div>
        </div>

        <!-- CTA (the whole card is the link, so this is a visual button) -->
        <div class="px-4 pb-4">
          <span
            class="flex w-full items-center justify-center gap-1.5 rounded-full bg-[#D96C2C] py-2.5 text-xs font-black text-white shadow-sm transition group-hover:bg-[#BF5720] sm:text-sm"
            :class="prod.quantity <= 0 ? 'bg-[#786B62]! group-hover:bg-[#786B62]!' : ''"
          >
            <i class="mdi text-base text-white" :class="prod.quantity <= 0 ? 'mdi-eye-outline' : 'mdi-cart-plus'"></i>
            <span class="text-white">{{ prod.quantity <= 0 ? 'ดูรายละเอียด' : 'สั่งซื้อสินค้า' }}</span>
          </span>
        </div>
      </RouterLink>
    </div>

    <!-- Empty state -->
    <div
      v-else
      class="space-y-2 rounded-[1.75rem] border-2 border-dashed border-[#E8D9C9] bg-[#FFF9F2] p-10 text-center"
    >
      <i class="mdi mdi-package-variant-remove text-3xl text-[#786B62]"></i>
      <p class="text-sm font-black text-[#332820]">ไม่พบสินค้าตามเงื่อนไขการค้นหา</p>
      <button
        v-if="productSearch || selectedCategory"
        type="button"
        class="cursor-pointer text-xs font-bold text-[#D96C2C] hover:underline sm:text-sm"
        @click="clearFilters"
      >
        ล้างตัวกรองทั้งหมด
      </button>
    </div>

    <!-- View all -->
    <div v-if="products.length > 0" class="pt-2">
      <RouterLink
        :to="`/shops/${shop.shopId}/products`"
        class="flex w-full items-center justify-center gap-2 rounded-full border-2 border-[#E8D9C9] bg-[#FFF9F2] py-3.5 text-sm font-black text-[#D96C2C] transition hover:border-[#D96C2C] hover:bg-[#D96C2C] hover:text-white focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none active:scale-[0.99] sm:text-base"
      >
        <i class="mdi mdi-grid text-xl"></i>
        <span>ดูสินค้าทั้งหมดของร้านนี้ ({{ products.length }} รายการ)</span>
        <i class="mdi mdi-arrow-right text-lg"></i>
      </RouterLink>
    </div>
  </section>
</template>

<style scoped>
.scrollbar-none::-webkit-scrollbar {
  display: none;
}
.scrollbar-none {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
