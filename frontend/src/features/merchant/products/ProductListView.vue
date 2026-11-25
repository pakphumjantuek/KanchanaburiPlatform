<script setup lang="ts">
import axios from 'axios'
import { computed, onMounted, ref } from 'vue'
import { getMyShop } from '@/features/merchant/api/shopApi'
import { getProducts, removeProduct, type Product } from '@/features/shops/api'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import { imageUrl } from '@/shared/utils/imageUrl'
import { formatCurrency } from '@/shared/utils/formatDate'
import ProductReviewsModal from './ProductReviewsModal.vue'

const reviewProduct = ref<Product | null>(null)

const products = ref<Product[]>([])
const hasShop = ref(false)
const shopId = ref<string>('')
const loading = ref(true)
const searchQuery = ref('')
const viewMode = ref<'grid' | 'table'>('grid')
const stockFilter = ref<'all' | 'inStock' | 'lowStock' | 'outOfStock'>('all')
const sortBy = ref<'newest' | 'priceAsc' | 'priceDesc' | 'stockAsc' | 'stockDesc'>('newest')
const swal = useSwal()

const totalCount = computed(() => products.value.length)
const inStockCount = computed(() => products.value.filter((p) => p.quantity > 5).length)
const lowStockCount = computed(() => products.value.filter((p) => p.quantity > 0 && p.quantity <= 5).length)
const outOfStockCount = computed(() => products.value.filter((p) => p.quantity === 0).length)

const filteredAndSortedProducts = computed(() => {
  let list = [...products.value]

  // Filter by stock
  if (stockFilter.value === 'inStock') {
    list = list.filter((p) => p.quantity > 5)
  } else if (stockFilter.value === 'lowStock') {
    list = list.filter((p) => p.quantity > 0 && p.quantity <= 5)
  } else if (stockFilter.value === 'outOfStock') {
    list = list.filter((p) => p.quantity === 0)
  }

  // Filter by search query
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase()
    list = list.filter(
      (p) =>
        p.productName.toLowerCase().includes(q) ||
        (p.description && p.description.toLowerCase().includes(q)),
    )
  }

  // Sorting
  if (sortBy.value === 'priceAsc') {
    list.sort((a, b) => a.price - b.price)
  } else if (sortBy.value === 'priceDesc') {
    list.sort((a, b) => b.price - a.price)
  } else if (sortBy.value === 'stockAsc') {
    list.sort((a, b) => a.quantity - b.quantity)
  } else if (sortBy.value === 'stockDesc') {
    list.sort((a, b) => b.quantity - a.quantity)
  }

  return list
})

async function load() {
  loading.value = true
  try {
    const shop = await getMyShop()
    hasShop.value = true
    shopId.value = shop.shopId
    products.value = (await getProducts()).filter((product) => product.shopId === shop.shopId)
  } catch (error) {
    if (!axios.isAxiosError(error) || error.response?.status !== 404) {
      await swal.error('โหลดสินค้าไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
    }
  } finally {
    loading.value = false
  }
}

async function closeProduct(product: Product) {
  const result = await swal.confirm(
    `ปิดการขาย ${product.productName}?`,
    'สินค้าจะไม่แสดงต่อสาธารณะบนแพลตฟอร์ม แต่ข้อมูลจะยังคงอยู่ในระบบ',
  )
  if (!result.isConfirmed) return
  try {
    await removeProduct(product.productId)
    products.value = products.value.filter((item) => item.productId !== product.productId)
    await swal.success('ปิดการขายสินค้าแล้ว')
  } catch (error) {
    await swal.error('ดำเนินการไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

onMounted(load)
</script>

<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12 text-slate-800">
    <!-- Skeleton Loading -->
    <div v-if="loading" class="space-y-6">
      <div class="h-28 w-full animate-pulse rounded-2xl bg-white border border-slate-200"></div>
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div v-for="i in 3" :key="i" class="h-24 animate-pulse rounded-2xl bg-white border border-slate-200"></div>
      </div>
      <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        <div
          v-for="i in 6"
          :key="i"
          class="h-72 animate-pulse rounded-2xl bg-white border border-slate-200"
        ></div>
      </div>
    </div>

    <template v-else-if="hasShop">
      <!-- 1. Header Banner Card -->
      <div
        class="flex flex-col sm:flex-row sm:items-center justify-between gap-5 rounded-2xl border border-slate-200/80 bg-white p-6 shadow-xs lg:px-8 lg:py-6"
      >
        <div class="flex items-center gap-4">
          <div
            class="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-600 text-white shadow-xs shrink-0"
          >
            <i class="mdi mdi-package-variant-closed text-2xl text-white"></i>
          </div>
          <div>
            <div class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-emerald-50 border border-emerald-200 text-xs font-bold text-emerald-800 mb-1">
              <i class="mdi mdi-tag-outline text-xs text-emerald-600"></i>
              <span>ศูนย์จัดการสินค้า • คลังสินค้าและราคา</span>
            </div>
            <div class="flex items-center gap-3">
              <h1 class="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
                จัดการรายการสินค้า
              </h1>
              <span
                class="rounded-full bg-emerald-100 text-emerald-800 px-3 py-0.5 text-xs font-bold border border-emerald-200"
              >
                {{ totalCount }} รายการ
              </span>
            </div>
            <p class="text-xs sm:text-sm text-slate-500 font-medium mt-0.5">
              จัดการข้อมูลสินค้า ราคาวางจำหน่าย คลังสต็อก โปรโมชัน และรีวิวจากลูกค้า
            </p>
          </div>
        </div>

        <RouterLink
          to="/my-shop/products/new"
          class="inline-flex items-center justify-center gap-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 px-5 py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 border border-emerald-600 cursor-pointer self-start sm:self-auto select-none"
        >
          <i class="mdi mdi-plus-circle text-lg text-white"></i>
          <span>เพิ่มสินค้าใหม่</span>
        </RouterLink>
      </div>

      <!-- 2. Quick Stock Overview KPI Cards -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <!-- Total Products Card -->
        <div class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs flex items-center justify-between">
          <div>
            <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">สินค้าทั้งหมดในร้าน</p>
            <h3 class="mt-1 text-2xl font-black text-slate-900 tracking-tight">
              {{ totalCount }}
              <span class="text-xs font-medium text-slate-500">รายการ</span>
            </h3>
          </div>
          <div class="flex h-11 w-11 items-center justify-center rounded-xl bg-emerald-50 text-emerald-700 border border-emerald-200 shrink-0">
            <i class="mdi mdi-package-variant text-2xl"></i>
          </div>
        </div>

        <!-- In Stock Card -->
        <div class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs flex items-center justify-between">
          <div>
            <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">สต็อกพร้อมขายปกติ</p>
            <h3 class="mt-1 text-2xl font-black text-emerald-700 tracking-tight">
              {{ inStockCount }}
              <span class="text-xs font-medium text-slate-500">รายการ</span>
            </h3>
          </div>
          <div class="flex h-11 w-11 items-center justify-center rounded-xl bg-emerald-50 text-emerald-700 border border-emerald-200 shrink-0">
            <i class="mdi mdi-check-circle-outline text-2xl"></i>
          </div>
        </div>

        <!-- Low Stock / Out of Stock Card -->
        <div class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs flex items-center justify-between">
          <div>
            <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">สต็อกต่ำ (≤5) / สินค้าหมด</p>
            <h3
              class="mt-1 text-2xl font-black tracking-tight"
              :class="lowStockCount + outOfStockCount > 0 ? 'text-rose-600' : 'text-slate-900'"
            >
              {{ lowStockCount + outOfStockCount }}
              <span class="text-xs font-medium text-slate-500">รายการ</span>
            </h3>
          </div>
          <div
            class="flex h-11 w-11 items-center justify-center rounded-xl shrink-0"
            :class="
              lowStockCount + outOfStockCount > 0
                ? 'bg-rose-50 text-rose-600 border border-rose-200'
                : 'bg-slate-50 text-slate-600 border border-slate-200'
            "
          >
            <i class="mdi mdi-alert-circle-outline text-2xl"></i>
          </div>
        </div>
      </div>

      <!-- 3. Search, Filter Pills & View Mode Bar -->
      <div class="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-xs space-y-3">
        <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-3">
          <!-- Search Input -->
          <div class="relative w-full lg:w-80">
            <input
              v-model="searchQuery"
              type="text"
              placeholder="ค้นหาชื่อหรือคำอธิบายสินค้า..."
              class="w-full rounded-xl border border-slate-200 bg-slate-50/60 pl-9 pr-8 py-2 text-xs sm:text-sm font-medium text-slate-900 outline-none focus:border-emerald-500 focus:bg-white transition"
            />
            <i
              class="mdi mdi-magnify absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-base"
            ></i>
            <button
              v-if="searchQuery"
              type="button"
              class="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-700 cursor-pointer"
              @click="searchQuery = ''"
            >
              <i class="mdi mdi-close-circle text-base"></i>
            </button>
          </div>

          <!-- Sort Select & View Mode Toggle -->
          <div class="flex flex-wrap items-center gap-2.5">
            <!-- Sort Selector -->
            <div class="flex items-center gap-1.5 text-xs text-slate-600 font-semibold">
              <span>เรียงตาม:</span>
              <select
                v-model="sortBy"
                class="rounded-xl border border-slate-200 bg-white px-3 py-1.5 text-xs font-bold text-slate-800 outline-none focus:border-emerald-500 cursor-pointer"
              >
                <option value="newest">ล่าสุด</option>
                <option value="priceAsc">ราคา: ต่ำไปสูง</option>
                <option value="priceDesc">ราคา: สูงไปต่ำ</option>
                <option value="stockAsc">สต็อก: น้อยไปมาก</option>
                <option value="stockDesc">สต็อก: มากไปน้อย</option>
              </select>
            </div>

            <!-- View Mode Switcher -->
            <div class="inline-flex rounded-xl bg-slate-100 p-1 border border-slate-200/70">
              <button
                type="button"
                @click="viewMode = 'grid'"
                class="px-2.5 py-1.5 rounded-lg text-xs font-bold transition flex items-center gap-1 cursor-pointer outline-none focus:outline-none"
                :class="viewMode === 'grid' ? 'bg-white text-slate-900 shadow-2xs' : 'text-slate-500 hover:text-slate-900'"
                title="มุมมองแบบการ์ด"
              >
                <i class="mdi mdi-view-grid-outline text-sm"></i>
                <span class="hidden sm:inline">การ์ด</span>
              </button>
              <button
                type="button"
                @click="viewMode = 'table'"
                class="px-2.5 py-1.5 rounded-lg text-xs font-bold transition flex items-center gap-1 cursor-pointer outline-none focus:outline-none"
                :class="viewMode === 'table' ? 'bg-white text-slate-900 shadow-2xs' : 'text-slate-500 hover:text-slate-900'"
                title="มุมมองแบบตาราง"
              >
                <i class="mdi mdi-format-list-bulleted text-sm"></i>
                <span class="hidden sm:inline">ตาราง</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Stock Quick Filter Pills -->
        <div class="flex flex-wrap items-center gap-2 pt-2 border-t border-slate-100">
          <button
            type="button"
            @click="stockFilter = 'all'"
            class="px-3 py-1 rounded-full text-xs font-bold transition cursor-pointer outline-none focus:outline-none flex items-center gap-1.5"
            :class="
              stockFilter === 'all'
                ? 'bg-slate-900 text-white shadow-xs'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200 hover:text-slate-900'
            "
          >
            <span>ทั้งหมด</span>
            <span
              class="rounded-full px-1.5 py-0.2 text-[10px] font-black"
              :class="stockFilter === 'all' ? 'bg-slate-700 text-white' : 'bg-slate-200 text-slate-700'"
            >
              {{ totalCount }}
            </span>
          </button>

          <button
            type="button"
            @click="stockFilter = 'inStock'"
            class="px-3 py-1 rounded-full text-xs font-bold transition cursor-pointer outline-none focus:outline-none flex items-center gap-1.5"
            :class="
              stockFilter === 'inStock'
                ? 'bg-emerald-600 text-white shadow-xs'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200 hover:text-slate-900'
            "
          >
            <span>พร้อมขายปกติ</span>
            <span
              class="rounded-full px-1.5 py-0.2 text-[10px] font-black"
              :class="stockFilter === 'inStock' ? 'bg-emerald-800 text-white' : 'bg-slate-200 text-slate-700'"
            >
              {{ inStockCount }}
            </span>
          </button>

          <button
            type="button"
            @click="stockFilter = 'lowStock'"
            class="px-3 py-1 rounded-full text-xs font-bold transition cursor-pointer outline-none focus:outline-none flex items-center gap-1.5"
            :class="
              stockFilter === 'lowStock'
                ? 'bg-amber-600 text-white shadow-xs'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200 hover:text-slate-900'
            "
          >
            <span>สต็อกต่ำ (≤5)</span>
            <span
              class="rounded-full px-1.5 py-0.2 text-[10px] font-black"
              :class="stockFilter === 'lowStock' ? 'bg-amber-800 text-white' : 'bg-slate-200 text-slate-700'"
            >
              {{ lowStockCount }}
            </span>
          </button>

          <button
            type="button"
            @click="stockFilter = 'outOfStock'"
            class="px-3 py-1 rounded-full text-xs font-bold transition cursor-pointer outline-none focus:outline-none flex items-center gap-1.5"
            :class="
              stockFilter === 'outOfStock'
                ? 'bg-rose-600 text-white shadow-xs'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200 hover:text-slate-900'
            "
          >
            <span>สินค้าหมด (0)</span>
            <span
              class="rounded-full px-1.5 py-0.2 text-[10px] font-black"
              :class="stockFilter === 'outOfStock' ? 'bg-rose-800 text-white' : 'bg-slate-200 text-slate-700'"
            >
              {{ outOfStockCount }}
            </span>
          </button>

          <span class="ml-auto text-xs text-slate-500 font-semibold hidden md:inline">
            แสดง {{ filteredAndSortedProducts.length }} จาก {{ products.length }} รายการ
          </span>
        </div>
      </div>

      <!-- 4. Empty State: No Products in Shop -->
      <div
        v-if="products.length === 0"
        class="flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-slate-200 bg-white py-16 px-6 text-center shadow-xs"
      >
        <div
          class="flex h-16 w-16 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600 mb-4 border border-emerald-200"
        >
          <i class="mdi mdi-package-variant text-3xl"></i>
        </div>
        <h2 class="text-xl font-bold text-slate-900">ยังไม่มีสินค้าในร้านค้าของคุณ</h2>
        <p class="mt-1.5 max-w-md text-xs sm:text-sm text-slate-500 font-medium leading-relaxed">
          เริ่มต้นเพิ่มรายการสินค้าชิ้นแรกของคุณเพื่อให้ลูกค้าสามารถเข้ามาเลือกซื้อผ่านแพลตฟอร์มได้ทันที
        </p>
        <RouterLink
          to="/my-shop/products/new"
          class="mt-6 inline-flex items-center gap-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 px-6 py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 border border-emerald-600"
        >
          <i class="mdi mdi-plus-circle text-lg text-white"></i>
          <span>เพิ่มสินค้าชิ้นแรก</span>
        </RouterLink>
      </div>

      <!-- 5. No Search Results State -->
      <div
        v-else-if="filteredAndSortedProducts.length === 0"
        class="rounded-2xl border border-slate-200 bg-white p-12 text-center shadow-xs space-y-3"
      >
        <div class="flex h-14 w-14 items-center justify-center rounded-full bg-slate-100 text-slate-400 mx-auto">
          <i class="mdi mdi-magnify-remove text-3xl"></i>
        </div>
        <p class="font-bold text-slate-900 text-base">ไม่พบสินค้าที่ตรงกับเงื่อนไขการค้นหา</p>
        <p class="text-xs text-slate-500 max-w-sm mx-auto">
          ลองเปลี่ยนคำค้นหา หรือรีเซ็ตตัวกรองสต็อกเพื่อดูสินค้าทั้งหมด
        </p>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-xl border border-slate-200 bg-slate-50 hover:bg-slate-100 px-4 py-2 text-xs font-bold text-slate-700 transition cursor-pointer"
          @click="searchQuery = ''; stockFilter = 'all'"
        >
          <i class="mdi mdi-refresh"></i>
          <span>รีเซ็ตตัวกรองทั้งหมด</span>
        </button>
      </div>

      <!-- 6. GRID VIEW -->
      <div v-else-if="viewMode === 'grid'" class="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        <article
          v-for="product in filteredAndSortedProducts"
          :key="product.productId"
          class="group flex flex-col overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-xs transition-all duration-300 hover:-translate-y-1 hover:border-emerald-300 hover:shadow-md"
        >
          <!-- Product Image Container -->
          <div class="relative h-48 w-full overflow-hidden bg-slate-900">
            <img
              v-if="product.imageUrl"
              :src="imageUrl(product.imageUrl)"
              :alt="product.productName"
              class="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
            />
            <div
              v-else
              class="flex h-full w-full flex-col items-center justify-center text-slate-400"
            >
              <i class="mdi mdi-image-off-outline text-4xl opacity-50"></i>
              <span class="mt-1 text-xs font-bold">ไม่มีรูปภาพ</span>
            </div>

            <!-- Floating Price Badge -->
            <div class="absolute bottom-3 left-3">
              <span
                class="inline-flex items-center rounded-xl bg-slate-900/85 backdrop-blur-xs px-3 py-1 text-xs font-bold text-white shadow-md border border-white/10"
              >
                {{ formatCurrency(product.price) }}
              </span>
            </div>

            <!-- Floating Stock Badge -->
            <div class="absolute top-3 right-3">
              <span
                v-if="product.quantity === 0"
                class="inline-flex items-center gap-1 rounded-full bg-rose-600 px-3 py-0.5 text-[10px] font-bold text-white shadow-xs"
              >
                สินค้าหมด
              </span>
              <span
                v-else-if="product.quantity <= 5"
                class="inline-flex items-center gap-1 rounded-full bg-amber-600 px-3 py-0.5 text-[10px] font-bold text-white shadow-xs"
              >
                สต็อกต่ำ: {{ product.quantity }} ชิ้น
              </span>
              <span
                v-else
                class="inline-flex items-center gap-1 rounded-full bg-emerald-600 px-3 py-0.5 text-[10px] font-bold text-white shadow-xs"
              >
                คลัง {{ product.quantity }} ชิ้น
              </span>
            </div>
          </div>

          <!-- Product Details Body -->
          <div class="flex flex-1 flex-col p-5 space-y-2">
            <h2
              class="text-base font-bold text-slate-900 group-hover:text-emerald-700 transition line-clamp-1"
            >
              {{ product.productName }}
            </h2>

            <p class="flex-1 line-clamp-2 text-xs text-slate-500 font-medium leading-relaxed min-h-[2rem]">
              {{ product.description || 'ไม่มีรายละเอียดสินค้า' }}
            </p>

            <!-- Action Buttons Bar -->
            <div
              class="mt-4 flex flex-wrap items-center justify-between gap-2 border-t border-slate-100 pt-3 text-xs"
            >
              <div class="flex items-center gap-3">
                <RouterLink
                  :to="`/products/${product.productId}`"
                  target="_blank"
                  class="inline-flex items-center gap-1 font-bold text-slate-600 hover:text-emerald-700 transition"
                  title="ดูหน้าสินค้าลูกค้า"
                >
                  <i class="mdi mdi-eye-outline text-sm text-slate-500"></i>
                  <span>ดูสินค้า</span>
                </RouterLink>

                <button
                  type="button"
                  class="inline-flex items-center gap-1 font-bold text-slate-600 hover:text-amber-600 transition cursor-pointer"
                  @click="reviewProduct = product"
                >
                  <i class="mdi mdi-star text-sm text-amber-500" aria-hidden="true"></i>
                  <span>รีวิว</span>
                </button>
              </div>

              <div class="flex flex-wrap items-center gap-1.5">
                <RouterLink
                  :to="`/my-shop/products/${product.productId}/deals`"
                  class="inline-flex items-center gap-1 rounded-lg border border-purple-200 bg-purple-50 px-2.5 py-1 text-xs font-bold text-purple-700 hover:bg-purple-600 hover:text-white transition shadow-2xs"
                  title="จัดการแคมเปญโปรโมชัน"
                >
                  <i class="mdi mdi-tag-outline text-xs"></i>
                  <span>โปร</span>
                </RouterLink>

                <RouterLink
                  :to="`/my-shop/products/${product.productId}/edit`"
                  class="inline-flex items-center gap-1 rounded-lg border border-emerald-200 bg-emerald-50 px-2.5 py-1 text-xs font-bold text-emerald-800 hover:bg-emerald-600 hover:text-white transition shadow-2xs"
                  title="แก้ไขข้อมูลสินค้า"
                >
                  <i class="mdi mdi-pencil-outline text-xs"></i>
                  <span>แก้ไข</span>
                </RouterLink>

                <button
                  type="button"
                  class="inline-flex items-center gap-1 rounded-lg border border-rose-200 bg-rose-50 px-2.5 py-1 text-xs font-bold text-rose-700 hover:bg-rose-600 hover:text-white transition shadow-2xs cursor-pointer"
                  title="ปิดการขายสินค้าชิ้นนี้"
                  @click="closeProduct(product)"
                >
                  <i class="mdi mdi-close-circle-outline text-xs"></i>
                  <span>ปิดขาย</span>
                </button>
              </div>
            </div>
          </div>
        </article>
      </div>

      <!-- 7. TABLE VIEW -->
      <div v-else class="overflow-x-auto rounded-2xl border border-slate-200/80 bg-white shadow-xs">
        <table class="w-full text-left text-xs sm:text-sm">
          <thead class="bg-slate-50 text-slate-600 font-bold border-b border-slate-200">
            <tr>
              <th class="py-3.5 px-4">สินค้า</th>
              <th class="py-3.5 px-4">ราคาจำหน่าย</th>
              <th class="py-3.5 px-4">สถานะคลังสต็อก</th>
              <th class="py-3.5 px-4 text-center">จัดการ</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr
              v-for="product in filteredAndSortedProducts"
              :key="product.productId"
              class="hover:bg-slate-50/70 transition"
            >
              <!-- Thumbnail & Title -->
              <td class="py-3.5 px-4">
                <div class="flex items-center gap-3">
                  <div class="h-12 w-12 rounded-xl bg-slate-900 overflow-hidden shrink-0 border border-slate-200">
                    <img
                      v-if="product.imageUrl"
                      :src="imageUrl(product.imageUrl)"
                      :alt="product.productName"
                      class="h-full w-full object-cover"
                    />
                    <div v-else class="flex h-full w-full items-center justify-center text-slate-500">
                      <i class="mdi mdi-image-off-outline text-lg"></i>
                    </div>
                  </div>
                  <div class="min-w-0">
                    <p class="font-bold text-slate-900 truncate max-w-xs sm:max-w-sm">{{ product.productName }}</p>
                    <p class="text-xs text-slate-400 truncate max-w-xs sm:max-w-sm">
                      {{ product.description || 'ไม่มีรายละเอียด' }}
                    </p>
                  </div>
                </div>
              </td>

              <!-- Price -->
              <td class="py-3.5 px-4 font-bold text-slate-900">
                {{ formatCurrency(product.price) }}
              </td>

              <!-- Stock Pill -->
              <td class="py-3.5 px-4">
                <span
                  v-if="product.quantity === 0"
                  class="inline-flex items-center gap-1 rounded-full bg-rose-50 border border-rose-200 text-rose-700 px-2.5 py-0.5 text-xs font-bold"
                >
                  <span class="h-1.5 w-1.5 rounded-full bg-rose-600"></span>
                  สินค้าหมด
                </span>
                <span
                  v-else-if="product.quantity <= 5"
                  class="inline-flex items-center gap-1 rounded-full bg-amber-50 border border-amber-200 text-amber-800 px-2.5 py-0.5 text-xs font-bold"
                >
                  <span class="h-1.5 w-1.5 rounded-full bg-amber-600"></span>
                  สต็อกต่ำ: {{ product.quantity }} ชิ้น
                </span>
                <span
                  v-else
                  class="inline-flex items-center gap-1 rounded-full bg-emerald-50 border border-emerald-200 text-emerald-800 px-2.5 py-0.5 text-xs font-bold"
                >
                  <span class="h-1.5 w-1.5 rounded-full bg-emerald-600"></span>
                  พร้อมขาย {{ product.quantity }} ชิ้น
                </span>
              </td>

              <!-- Actions -->
              <td class="py-3.5 px-4">
                <div class="flex items-center justify-center gap-1.5">
                  <RouterLink
                    :to="`/products/${product.productId}`"
                    target="_blank"
                    class="p-1.5 rounded-lg text-slate-500 hover:text-slate-800 hover:bg-slate-100 transition"
                    title="ดูหน้าสินค้าลูกค้า"
                  >
                    <i class="mdi mdi-eye-outline text-base"></i>
                  </RouterLink>

                  <button
                    type="button"
                    class="p-1.5 rounded-lg text-amber-500 hover:text-amber-700 hover:bg-amber-50 transition cursor-pointer"
                    title="ดูรีวิวสินค้า"
                    @click="reviewProduct = product"
                  >
                    <i class="mdi mdi-star-outline text-base"></i>
                  </button>

                  <RouterLink
                    :to="`/my-shop/products/${product.productId}/deals`"
                    class="px-2.5 py-1 rounded-lg border border-purple-200 bg-purple-50 text-purple-700 text-xs font-bold hover:bg-purple-600 hover:text-white transition"
                    title="จัดการดีลโปรโมชัน"
                  >
                    โปรโมชัน
                  </RouterLink>

                  <RouterLink
                    :to="`/my-shop/products/${product.productId}/edit`"
                    class="px-2.5 py-1 rounded-lg border border-emerald-200 bg-emerald-50 text-emerald-800 text-xs font-bold hover:bg-emerald-600 hover:text-white transition"
                    title="แก้ไขสินค้า"
                  >
                    แก้ไข
                  </RouterLink>

                  <button
                    type="button"
                    class="p-1.5 rounded-lg text-rose-600 hover:bg-rose-50 transition cursor-pointer"
                    title="ปิดการขาย"
                    @click="closeProduct(product)"
                  >
                    <i class="mdi mdi-close-circle-outline text-base"></i>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <!-- State: Merchant Has No Shop Yet -->
    <div
      v-else
      class="flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-slate-200 bg-white py-16 px-6 text-center shadow-xs space-y-3"
    >
      <div
        class="flex h-16 w-16 items-center justify-center rounded-2xl bg-amber-50 text-amber-700 mb-2 border border-amber-200"
      >
        <i class="mdi mdi-store-plus-outline text-3xl"></i>
      </div>
      <h2 class="text-xl font-bold text-slate-900">กรุณาสร้างร้านค้าก่อนจัดการสินค้า</h2>
      <p class="max-w-md text-xs sm:text-sm text-slate-500 font-medium leading-relaxed">
        คุณจำเป็นต้องตั้งค่าข้อมูลร้านค้าเบื้องต้นก่อนจึงจะสามารถลงรายการสินค้าสำหรับวางจำหน่ายได้
      </p>
      <RouterLink
        to="/my-shop"
        class="mt-4 inline-flex items-center gap-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 px-6 py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 border border-emerald-600"
      >
        <i class="mdi mdi-store-cog text-lg text-white"></i>
        <span>ไปยังหน้าตั้งค่าร้านค้า</span>
      </RouterLink>
    </div>

    <!-- Product Reviews Modal -->
    <ProductReviewsModal
      v-if="reviewProduct"
      :key="reviewProduct.productId"
      :product-id="reviewProduct.productId"
      :product-name="reviewProduct.productName"
      @close="reviewProduct = null"
    />
  </div>
</template>

<style scoped>
button,
a,
input,
select,
[role="button"] {
  outline: none !important;
  -webkit-tap-highlight-color: transparent !important;
}

button:focus,
button:focus-visible,
button:active,
a:focus,
a:focus-visible,
a:active,
input:focus,
input:focus-visible,
input:active,
select:focus,
select:focus-visible,
select:active {
  outline: none !important;
  box-shadow: none !important;
}
</style>

