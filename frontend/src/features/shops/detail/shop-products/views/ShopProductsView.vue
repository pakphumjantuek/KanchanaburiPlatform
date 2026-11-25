<template>
  <div class="min-h-screen bg-[#F7F0E6] pb-24 text-[#332820]">
    <!-- BREADCRUMB -->
    <div class="border-b-2 border-[#E8D9C9] bg-[#FFF9F2] py-3.5 shadow-2xs">
      <div
        class="scrollbar-none mx-auto flex w-full max-w-[1800px] items-center gap-2 overflow-x-auto px-4 text-xs font-bold whitespace-nowrap text-[#786B62] sm:px-6 lg:px-10"
      >
        <RouterLink to="/" class="transition hover:text-[#D96C2C]">หน้าแรก</RouterLink>
        <i class="mdi mdi-chevron-right text-[#E8D9C9]"></i>
        <RouterLink to="/shops" class="transition hover:text-[#D96C2C]">ร้านค้าทั้งหมด</RouterLink>
        <i class="mdi mdi-chevron-right text-[#E8D9C9]"></i>
        <RouterLink
          v-if="shop"
          :to="`/shops/${shop.shopId}`"
          class="line-clamp-1 transition hover:text-[#D96C2C]"
          >{{ shop.shopName }}</RouterLink
        >
        <i class="mdi mdi-chevron-right text-[#E8D9C9]"></i>
        <span class="font-black text-[#D96C2C]">สินค้าทั้งหมด</span>
      </div>
    </div>

    <!-- HEADER -->
    <section
      v-if="shop"
      class="relative overflow-hidden border-b-2 border-[#E8D9C9] bg-gradient-to-br from-white via-[#FFF9F2] to-[#F7F0E6] py-8 sm:py-10"
    >
      <div class="pointer-events-none absolute -top-24 -right-16 h-72 w-72 rounded-full bg-[#F2A65A]/25 blur-3xl"></div>
      <div class="pointer-events-none absolute -bottom-28 left-1/3 h-64 w-64 rounded-full bg-[#D96C2C]/15 blur-3xl"></div>

      <div class="relative mx-auto w-full max-w-[1800px] px-4 sm:px-6 lg:px-10">
        <div class="flex flex-col justify-between gap-6 md:flex-row md:items-center">
          <div class="flex items-center gap-4 sm:gap-5">
            <img
              v-if="imageUrl(shop.coverImageUrl)"
              :src="imageUrl(shop.coverImageUrl)"
              :alt="shop.shopName"
              class="h-16 w-16 shrink-0 rounded-3xl border-4 border-white object-cover shadow-xl ring-4 ring-[#D96C2C]/20 sm:h-24 sm:w-24"
            />
            <div
              v-else
              class="flex h-16 w-16 shrink-0 items-center justify-center rounded-3xl border-4 border-white bg-[#FFF9F2] text-3xl text-[#D96C2C] shadow-xl ring-4 ring-[#D96C2C]/20 sm:h-24 sm:w-24 sm:text-4xl"
            >
              <i class="mdi mdi-storefront-outline"></i>
            </div>

            <div class="min-w-0 space-y-2">
              <div class="flex flex-wrap items-center gap-2">
                <span
                  class="rounded-full border border-[#E8D9C9] bg-white px-3 py-0.5 text-[11px] font-black text-[#D96C2C] shadow-2xs"
                >
                  {{ shop.categoryName || 'ร้านค้าชุมชน' }}
                </span>
                <span class="flex items-center gap-1 text-xs font-semibold text-[#786B62]">
                  <i class="mdi mdi-map-marker text-sm text-[#D96C2C]"></i>
                  {{ shopArea }}
                </span>
              </div>
              <h1
                class="text-2xl leading-tight font-black tracking-tight text-[#332820] sm:text-3xl lg:text-4xl"
              >
                สินค้าทั้งหมดของ
                <span class="text-[#D96C2C]">{{ shop.shopName }}</span>
              </h1>
              <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
                มีสินค้าทั้งหมด
                <span class="font-black text-[#D96C2C]">{{ products.length }}</span> รายการ
              </p>
            </div>
          </div>

          <RouterLink
            :to="`/shops/${shop.shopId}`"
            class="inline-flex shrink-0 items-center justify-center gap-2 self-start rounded-full bg-[#D96C2C] px-6 py-3 text-xs font-black text-white shadow-lg shadow-[#D96C2C]/30 transition hover:bg-[#BF5720] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95 sm:text-sm md:self-auto"
          >
            <i class="mdi mdi-arrow-left text-base text-white"></i>
            <span class="text-white">กลับไปหน้าร้านค้า</span>
          </RouterLink>
        </div>
      </div>
    </section>

    <!-- MAIN -->
    <main class="mx-auto w-full max-w-[1800px] space-y-6 px-4 py-6 sm:px-6 sm:py-8 lg:px-10">
      <!-- Toolbar -->
      <div
        class="space-y-4 rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white p-5 shadow-md shadow-[#D96C2C]/5 sm:p-6"
      >
        <div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div
            class="flex w-full items-center gap-1.5 rounded-2xl border-2 border-[#E8D9C9] bg-white p-1.5 shadow-sm transition focus-within:border-[#D96C2C] focus-within:shadow-md focus-within:ring-4 focus-within:ring-[#D96C2C]/15 md:max-w-md"
          >
            <span
              class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#FFF9F2] text-lg text-[#D96C2C]"
            >
              <i class="mdi mdi-magnify"></i>
            </span>
            <input
              v-model="searchQuery"
              type="search"
              autocomplete="off"
              placeholder="ค้นหาสินค้าในร้านนี้..."
              aria-label="ค้นหาสินค้าในร้านนี้"
              class="min-w-0 flex-1 bg-transparent py-2 text-sm font-bold text-[#332820] outline-none placeholder:font-medium placeholder:text-[#A8988C] [&::-webkit-search-cancel-button]:hidden"
            />
            <button
              v-if="searchQuery"
              type="button"
              class="mr-1 flex h-7 w-7 shrink-0 cursor-pointer items-center justify-center rounded-full text-[#786B62] transition hover:bg-[#F7F0E6] hover:text-[#332820]"
              aria-label="ล้างคำค้นหา"
              @click="searchQuery = ''"
            >
              <i class="mdi mdi-close text-base"></i>
            </button>
          </div>

          <div class="flex flex-wrap items-center gap-1.5" role="radiogroup" aria-label="เรียงลำดับ">
            <span class="mr-0.5 text-xs font-bold text-[#786B62]">เรียง:</span>
            <button
              v-for="opt in sortChoices"
              :key="opt.value"
              type="button"
              role="radio"
              :aria-checked="sortBy === opt.value"
              class="shrink-0 cursor-pointer rounded-full border-2 px-3.5 py-1.5 text-xs font-black whitespace-nowrap transition select-none focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none active:scale-95"
              :class="
                sortBy === opt.value
                  ? 'border-[#D96C2C] bg-[#D96C2C] text-white shadow-sm'
                  : 'border-[#E8D9C9] bg-white text-[#332820] hover:border-[#D96C2C]'
              "
              @click="sortBy = opt.value"
            >
              {{ opt.label }}
            </button>
          </div>
        </div>

        <div
          v-if="productCategories.length > 1"
          class="scrollbar-none flex items-center gap-2 overflow-x-auto border-t-2 border-[#E8D9C9] pt-4 pb-1"
        >
          <button
            v-for="cat in productCategories"
            :key="cat.name"
            type="button"
            class="flex shrink-0 cursor-pointer items-center gap-1.5 rounded-full border-2 px-4 py-2 text-xs font-black shadow-2xs transition select-none focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none active:scale-95"
            :class="
              selectedCategory === cat.id
                ? 'border-[#D96C2C] bg-[#D96C2C] text-white shadow-sm'
                : 'border-[#E8D9C9] bg-[#FFF9F2] text-[#332820] hover:border-[#D96C2C]'
            "
            @click="selectedCategory = cat.id"
          >
            <span>{{ cat.name }}</span>
            <span
              class="rounded-full px-2 py-0.5 text-[11px]"
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
      </div>

      <p v-if="!loading && products.length" class="px-1 text-xs font-bold text-[#786B62] sm:text-sm">
        แสดง <span class="font-black text-[#D96C2C]">{{ filteredProducts.length }}</span> จาก
        {{ products.length }} รายการ
      </p>

      <!-- Skeleton -->
      <div
        v-if="loading"
        class="grid grid-cols-2 gap-4 sm:grid-cols-3 sm:gap-5 lg:grid-cols-4 xl:grid-cols-5"
      >
        <div
          v-for="i in 10"
          :key="i"
          class="animate-pulse overflow-hidden rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white shadow-xs"
        >
          <div class="aspect-[4/3] w-full bg-[#F7F0E6]"></div>
          <div class="space-y-3 p-4">
            <div class="h-3 w-1/2 rounded-full bg-[#F7F0E6]"></div>
            <div class="h-4 w-4/5 rounded-full bg-[#E8D9C9]"></div>
            <div class="h-5 w-1/3 rounded-full bg-[#E8D9C9]"></div>
          </div>
        </div>
      </div>

      <!-- Empty -->
      <div
        v-else-if="!filteredProducts.length"
        class="space-y-3 rounded-[1.75rem] border-2 border-dashed border-[#E8D9C9] bg-white px-6 py-20 text-center"
      >
        <div
          class="mx-auto flex h-20 w-20 items-center justify-center rounded-full border border-[#E8D9C9] bg-[#FFF9F2] text-[#D96C2C]"
        >
          <i class="mdi mdi-package-variant-remove text-4xl"></i>
        </div>
        <h3 class="text-xl font-black text-[#332820]">
          {{ products.length ? 'ไม่พบสินค้าที่ตรงกับตัวกรอง' : 'ร้านนี้ยังไม่มีสินค้า' }}
        </h3>
        <p class="mx-auto max-w-md text-xs font-semibold text-[#786B62] sm:text-sm">
          {{
            products.length
              ? 'ลองเปลี่ยนคำค้นหา หรือเลือกหมวดหมู่อื่นเพื่อดูสินค้าเพิ่มเติม'
              : 'ร้านค้านี้ยังไม่ได้เพิ่มสินค้าในขณะนี้'
          }}
        </p>
        <button
          v-if="searchQuery || selectedCategory"
          type="button"
          class="mt-2 inline-flex cursor-pointer items-center gap-2 rounded-full bg-[#D96C2C] px-6 py-2.5 text-xs font-black text-white shadow-md transition hover:bg-[#BF5720] active:scale-95"
          @click="clearFilters"
        >
          <i class="mdi mdi-refresh text-base text-white"></i>
          <span class="text-white">ล้างตัวกรองทั้งหมด</span>
        </button>
      </div>

      <!-- Grid -->
      <div
        v-else
        class="grid grid-cols-2 gap-4 sm:grid-cols-3 sm:gap-5 lg:grid-cols-4 xl:grid-cols-5"
      >
        <RouterLink
          v-for="(prod, idx) in filteredProducts"
          :key="prod.productId"
          v-reveal
          :to="`/products/${prod.productId}`"
          :style="{ animationDelay: `${(idx % 4) * 80}ms` }"
          class="group flex flex-col justify-between overflow-hidden rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white shadow-xs transition-all duration-300 ease-out hover:-translate-y-1.5 hover:border-[#D96C2C] hover:shadow-2xl hover:shadow-[#D96C2C]/10 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
        >
          <div>
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
              <p
                v-if="prod.description"
                class="line-clamp-2 text-xs leading-relaxed font-medium text-[#786B62]"
              >
                {{ prod.description }}
              </p>
            </div>
          </div>

          <div
            class="flex items-center justify-between gap-2 border-t border-[#E8D9C9] bg-[#FFF9F2] px-4 py-3.5"
          >
            <div class="min-w-0">
              <span class="block text-[10px] font-bold text-[#786B62]">ราคา</span>
              <div class="flex flex-wrap items-baseline gap-x-2">
                <span class="text-base font-black text-[#D96C2C] sm:text-lg">{{
                  formatPrice(dealPrice(prod))
                }}</span>
                <span v-if="prod.activeDeal" class="text-xs font-semibold text-[#786B62] line-through">
                  {{ formatPrice(prod.price) }}
                </span>
              </div>
            </div>
            <span
              class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-[#D96C2C] text-white shadow-md transition group-hover:bg-[#BF5720]"
              :class="prod.quantity <= 0 ? 'bg-[#786B62]! group-hover:bg-[#786B62]!' : ''"
              :title="prod.quantity <= 0 ? 'ดูรายละเอียด' : 'สั่งซื้อสินค้า'"
            >
              <i class="mdi text-lg text-white" :class="prod.quantity <= 0 ? 'mdi-eye-outline' : 'mdi-cart-plus'"></i>
            </span>
          </div>
        </RouterLink>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  getProductCategories,
  getShop,
  getShopProducts,
  type Product,
  type ProductCategory,
  type Shop,
} from '@/features/shops/api'
import { push } from 'notivue'
import { dealLabel, dealPrice } from '@/shared/utils/productDeal'
import { vReveal } from '@/shared/directives/reveal'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'

const route = useRoute()
const router = useRouter()

const shop = ref<Shop | null>(null)
const products = ref<Product[]>([])
const categories = ref<ProductCategory[]>([])
const loading = ref(true)

const selectedCategory = ref<string | null>(null)
const searchQuery = ref('')
type SortKey = 'default' | 'price-asc' | 'price-desc' | 'name'
const sortBy = ref<SortKey>('default')
const sortChoices: { value: SortKey; label: string }[] = [
  { value: 'default', label: 'แนะนำ' },
  { value: 'price-asc', label: 'ราคาต่ำ-สูง' },
  { value: 'price-desc', label: 'ราคาสูง-ต่ำ' },
  { value: 'name', label: 'ชื่อ A-Z' },
]

function clearFilters() {
  searchQuery.value = ''
  selectedCategory.value = null
}

const apiOrigin = (import.meta.env.VITE_API_URL ?? 'https://localhost:7289/api').replace(
  /\/api$/,
  '',
)

function imageUrl(url?: string) {
  return url?.startsWith('/') ? `${apiOrigin}${url}` : url
}

function formatPrice(value: number) {
  return new Intl.NumberFormat('th-TH', {
    style: 'currency',
    currency: 'THB',
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value)
}

function getProductImage(product: Product, _index: number): string {
  return imageUrl(product.imageUrl) || ''
}

const shopArea = computed(() => {
  const subDistrict = shop.value?.subDistrictName
  const district = shop.value?.districtName
  return [subDistrict ? `ต.${subDistrict}` : null, district ? `อ.${district}` : null, 'จ.กาญจนบุรี']
    .filter(Boolean)
    .join(' ')
})

const productCategories = computed(() => [
  { id: null, name: 'ทั้งหมด', count: products.value.length },
  ...categories.value
    .filter((category) =>
      products.value.some((product) => product.productCategoryId === category.productCategoryId),
    )
    .map((category) => ({
      id: category.productCategoryId,
      name: category.categoryName,
      count: products.value.filter((p) => p.productCategoryId === category.productCategoryId)
        .length,
    })),
])

function productCategoryName(productCategoryId: string) {
  return (
    categories.value.find((category) => category.productCategoryId === productCategoryId)
      ?.categoryName ?? 'สินค้า'
  )
}

const filteredProducts = computed(() => {
  let list = [...products.value]
  if (selectedCategory.value) {
    list = list.filter((p) => p.productCategoryId === selectedCategory.value)
  }
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim()
    list = list.filter(
      (p) =>
        p.productName.toLowerCase().includes(q) ||
        (p.description && p.description.toLowerCase().includes(q)),
    )
  }

  if (sortBy.value === 'price-asc') {
    list.sort((a, b) => dealPrice(a) - dealPrice(b))
  } else if (sortBy.value === 'price-desc') {
    list.sort((a, b) => dealPrice(b) - dealPrice(a))
  } else if (sortBy.value === 'name') {
    list.sort((a, b) => a.productName.localeCompare(b.productName, 'th'))
  }

  return list
})

onMounted(async () => {
  loading.value = true
  try {
    const shopId = String(route.params.id)
    const [shopData, productsRes, catData] = await Promise.all([
      getShop(shopId),
      getShopProducts(shopId),
      getProductCategories(),
    ])
    shop.value = shopData
    products.value = productsRes || []
    categories.value = catData || []
  } catch (error) {
    push.error({
      title: 'ไม่พบร้านค้า',
      message: getApiErrorMessage(error, 'ร้านค้านี้อาจถูกปิดการใช้งาน'),
    })
    await router.replace('/shops')
  } finally {
    loading.value = false
  }
})
</script>
<style scoped>
.scrollbar-none::-webkit-scrollbar {
  display: none;
}
.scrollbar-none {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
