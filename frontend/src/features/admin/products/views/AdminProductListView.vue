<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter, RouterLink } from 'vue-router'
import {
  getAdminProducts,
  updateAdminProductStatus,
} from '../api/adminProductApi'
import { getAdminShop } from '@/features/admin/shops/api/adminShopApi'
import { getProductCategories } from '@/features/shops/api/productApi'
import type { Product, ProductCategory } from '@/features/shops/shared/types/product'
import type { Shop } from '@/features/shops/shared/types/shop'
import AppDataTable, {
  type DataTableColumn,
  type DataTablePagination,
} from '@/components/common/AppDataTable.vue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'

const route = useRoute()
const router = useRouter()
const swal = useSwal()

const shopId = computed(() => (typeof route.params.shopId === 'string' ? route.params.shopId : ''))
const shop = ref<Shop | null>(null)
const shopPublicUrl = computed(() => router.resolve(`/shops/${shopId.value}`).href)
const loadingShop = ref(false)

const products = ref<Product[]>([])
const categories = ref<ProductCategory[]>([])
const loading = ref(true)
const updatingId = ref<string | null>(null)

// Filters & Tabs
const activeTab = ref<'All' | 'Active' | 'Inactive'>('All')
const search = ref('')
const selectedCategoryId = ref<string>('')

// KPI Stats
const stats = ref({
  all: 0,
  active: 0,
  inactive: 0,
  totalStock: 0,
})

// Pagination
const pagination = ref<DataTablePagination>({
  page: 1,
  pageSize: 10,
  totalCount: 0,
  totalPages: 0,
})

// Inspection Modal
const selectedProduct = ref<Product | null>(null)
const activePreviewImage = ref<string>('')

const columns: DataTableColumn[] = [
  { key: 'product', label: 'สินค้า', class: 'font-bold text-slate-900 min-w-[280px]' },
  { key: 'category', label: 'หมวดหมู่', class: 'text-slate-700' },
  { key: 'price', label: 'ราคาต่อหน่วย' },
  { key: 'quantity', label: 'สต็อกคงเหลือ' },
  { key: 'status', label: 'สถานะการขาย' },
  { key: 'actions', label: 'จัดการ', class: 'text-right' },
]

const statusTabs = [
  { value: 'All', label: 'ทั้งหมด', icon: 'mdi-package-variant-closed' },
  { value: 'Active', label: 'เปิดขายปกติ', icon: 'mdi-check-circle-outline' },
  { value: 'Inactive', label: 'ปิดการขาย', icon: 'mdi-pause-circle-outline' },
]

const kpiCards = computed(() => [
  {
    key: 'all',
    tab: 'All' as const,
    label: 'สินค้าทั้งหมด',
    hint: 'ทุกรายการในร้านนี้',
    value: stats.value.all,
    icon: 'mdi-package-variant-closed',
    bar: 'bg-slate-600',
    text: 'text-slate-900',
    iconBox: 'bg-slate-100 text-slate-700',
    activeBorder: 'border-slate-700',
    ring: 'ring-slate-500/15',
  },
  {
    key: 'active',
    tab: 'Active' as const,
    label: 'เปิดขายปกติ',
    hint: 'พร้อมจำหน่ายหน้าร้าน',
    value: stats.value.active,
    icon: 'mdi-check-circle-outline',
    bar: 'bg-emerald-600',
    text: 'text-emerald-700',
    iconBox: 'bg-emerald-100 text-emerald-700',
    activeBorder: 'border-emerald-600',
    ring: 'ring-emerald-500/15',
  },
  {
    key: 'inactive',
    tab: 'Inactive' as const,
    label: 'ปิดการขาย',
    hint: 'หยุดขายหรือปิดชั่วคราว',
    value: stats.value.inactive,
    icon: 'mdi-pause-circle-outline',
    bar: 'bg-slate-400',
    text: 'text-slate-700',
    iconBox: 'bg-slate-100 text-slate-600',
    activeBorder: 'border-slate-500',
    ring: 'ring-slate-400/15',
  },
  {
    key: 'stock',
    tab: null,
    label: 'สต็อกคงเหลือรวม',
    hint: 'จำนวนหน่วยสินค้าในคลัง',
    value: stats.value.totalStock,
    icon: 'mdi-archive-outline',
    bar: 'bg-teal-600',
    text: 'text-teal-800',
    iconBox: 'bg-teal-100 text-teal-700',
    activeBorder: 'border-teal-600',
    ring: 'ring-teal-500/15',
  },
])

const categoryMap = computed(() => {
  const map = new Map<string, string>()
  for (const cat of categories.value) {
    map.set(cat.productCategoryId, cat.categoryName)
  }
  return map
})

function getCategoryName(catId?: string) {
  if (!catId) return 'ไม่ระบุหมวดหมู่'
  return categoryMap.value.get(catId) || 'ทั่วไป'
}

function asProduct(item: object): Product {
  return item as Product
}

function formatCurrency(amount?: number) {
  if (amount === undefined || amount === null) return '0 บาท'
  return `${Number(amount).toLocaleString('th-TH')} บาท`
}

function formatDate(dateStr?: string) {
  if (!dateStr) return '-'
  try {
    return new Date(dateStr).toLocaleDateString('th-TH', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  } catch {
    return dateStr
  }
}

function stockStatus(qty: number) {
  if (qty <= 0) {
    return {
      text: 'สินค้าหมด (0 ชิ้น)',
      badgeClass: 'bg-rose-100 text-rose-800 border-rose-300',
      dotClass: 'bg-rose-500',
    }
  }
  if (qty <= 5) {
    return {
      text: `ใกล้หมด (${qty} ชิ้น)`,
      badgeClass: 'bg-amber-100 text-amber-800 border-amber-300',
      dotClass: 'bg-amber-500',
    }
  }
  return {
    text: `พร้อมจำหน่าย (${qty} ชิ้น)`,
    badgeClass: 'bg-emerald-100 text-emerald-800 border-emerald-300',
    dotClass: 'bg-emerald-600',
  }
}

// Client-side filtering when search or category filter is active
const displayProducts = computed(() => {
  let list = products.value

  const q = search.value.trim().toLowerCase()
  if (q) {
    list = list.filter((p) => {
      const matchName = p.productName?.toLowerCase().includes(q)
      const matchId = p.productId?.toLowerCase().includes(q)
      const catName = getCategoryName(p.productCategoryId).toLowerCase()
      const matchCat = catName.includes(q)
      const matchDesc = p.description?.toLowerCase().includes(q)
      return matchName || matchId || matchCat || matchDesc
    })
  }

  if (selectedCategoryId.value) {
    list = list.filter((p) => p.productCategoryId === selectedCategoryId.value)
  }

  return list
})

async function loadShopInfo() {
  if (!shopId.value) return
  loadingShop.value = true
  try {
    shop.value = await getAdminShop(shopId.value)
  } catch (error) {
    console.error('Failed to load shop details', error)
  } finally {
    loadingShop.value = false
  }
}

async function loadCategories() {
  try {
    categories.value = await getProductCategories()
  } catch (error) {
    console.error('Failed to load categories', error)
    categories.value = []
  }
}

async function loadStats() {
  if (!shopId.value) return
  try {
    const [allRes, activeRes, inactiveRes] = await Promise.all([
      getAdminProducts({ shopId: shopId.value, page: 1, pageSize: 100 }),
      getAdminProducts({ shopId: shopId.value, status: 'Active', page: 1, pageSize: 1 }),
      getAdminProducts({ shopId: shopId.value, status: 'Inactive', page: 1, pageSize: 1 }),
    ])
    stats.value.all = allRes.totalCount
    stats.value.active = activeRes.totalCount
    stats.value.inactive = inactiveRes.totalCount
    stats.value.totalStock = allRes.items.reduce((sum, p) => sum + (p.quantity || 0), 0)
  } catch (error) {
    console.error('Failed to load stats', error)
  }
}

async function loadProducts() {
  if (!shopId.value) return
  loading.value = true
  try {
    const result = await getAdminProducts({
      shopId: shopId.value,
      status: activeTab.value === 'All' ? null : activeTab.value,
      page: pagination.value.page,
      pageSize: pagination.value.pageSize,
    })
    products.value = result.items
    pagination.value = result
  } catch (error) {
    await swal.error('โหลดข้อมูลสินค้าไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}

async function refreshAll() {
  await Promise.all([loadShopInfo(), loadCategories(), loadStats(), loadProducts()])
}

function selectTab(tab: 'All' | 'Active' | 'Inactive') {
  activeTab.value = tab
  pagination.value.page = 1
  loadProducts()
}

function changePage(page: number) {
  pagination.value.page = page
  loadProducts()
}

function clearFilters() {
  search.value = ''
  selectedCategoryId.value = ''
}

function openProductDetail(product: Product) {
  selectedProduct.value = product
  activePreviewImage.value = product.imageUrl || (product.detailImages && product.detailImages[0]) || ''
}

function closeProductDetail() {
  selectedProduct.value = null
  activePreviewImage.value = ''
}

async function toggleProductStatus(product: Product) {
  const isCurrentlyActive = product.status === 'Active'
  const targetStatus = isCurrentlyActive ? 'Inactive' : 'Active'

  const title = isCurrentlyActive
    ? `ยืนยันปิดการขาย "${product.productName}"?`
    : `ยืนยันเปิดการขาย "${product.productName}"?`
  const text = isCurrentlyActive
    ? 'สินค้าจะไม่แสดงในหน้าหลักและหน้าร้านของระบบชั่วคราว'
    : 'สินค้าจะกลับมาแสดงในหน้าหลักและพร้อมจำหน่ายตามปกติ'

  const result = await swal.confirm(title, text)
  if (!result.isConfirmed) return

  updatingId.value = product.productId
  try {
    await updateAdminProductStatus(product.productId, targetStatus)
    product.status = targetStatus

    if (selectedProduct.value && selectedProduct.value.productId === product.productId) {
      selectedProduct.value.status = targetStatus
    }

    await swal.success(
      targetStatus === 'Active' ? 'เปิดการขายสินค้าเรียบร้อยแล้ว' : 'ปิดการขายสินค้าเรียบร้อยแล้ว'
    )
    await loadStats()
  } catch (error) {
    await swal.error('ดำเนินการไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    updatingId.value = null
  }
}

watch(activeTab, () => {
  pagination.value.page = 1
})

onMounted(() => {
  refreshAll()
})
</script>

<template>
  <main class="mx-auto w-full max-w-[1920px] space-y-6 px-4 py-6 sm:px-6 lg:px-10 lg:py-8">
    <!-- BREADCRUMB -->
    <nav class="flex items-center gap-2 text-sm font-bold text-slate-600">
      <RouterLink
        to="/admin/shops"
        class="inline-flex items-center gap-1.5 transition hover:text-teal-700"
      >
        <i class="mdi mdi-arrow-left text-base" />
        <span>ร้านค้า</span>
      </RouterLink>
      <i class="mdi mdi-chevron-right text-base text-slate-400" />
      <span class="text-slate-900">สินค้าในร้าน</span>
    </nav>

    <!-- SHOP HEADER BANNER -->
    <section
      class="relative overflow-hidden rounded-3xl bg-gradient-to-br from-slate-900 via-slate-800 to-teal-900 p-6 text-white shadow-lg sm:p-8"
    >
      <div class="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full bg-teal-400/20 blur-3xl" />
      <div class="pointer-events-none absolute -bottom-24 left-1/3 h-64 w-64 rounded-full bg-cyan-400/10 blur-3xl" />

      <div class="relative z-10 flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
        <div class="flex items-center gap-4 sm:gap-5">
          <div
            class="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-2xl border border-white/25 bg-white/10 text-3xl text-teal-300 backdrop-blur-md sm:h-20 sm:w-20"
          >
            <img
              v-if="shop?.coverImageUrl"
              :src="shop.coverImageUrl"
              :alt="shop.shopName"
              class="h-full w-full object-cover"
            />
            <i v-else class="mdi mdi-storefront-outline" />
          </div>

          <div class="min-w-0">
            <p class="text-xs font-black tracking-wide text-teal-300">สินค้าในร้านค้า</p>
            <h1 class="truncate text-2xl font-black tracking-tight sm:text-3xl">
              {{ shop?.shopName || 'กำลังโหลดข้อมูลร้านค้า...' }}
            </h1>
            <div class="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1.5 text-xs font-bold text-slate-300">
              <span
                v-if="shop?.categoryName"
                class="rounded-lg border border-white/20 bg-white/10 px-2.5 py-0.5 text-white"
              >
                {{ shop.categoryName }}
              </span>
              <span
                v-if="shop?.status"
                class="rounded-full px-2.5 py-0.5 font-black"
                :class="
                  shop.status === 'Active'
                    ? 'bg-emerald-100 text-emerald-800'
                    : 'bg-slate-200 text-slate-800'
                "
              >
                {{ shop.status === 'Active' ? 'เปิดทำการ' : shop.status }}
              </span>
              <span v-if="shop?.phone" class="inline-flex items-center gap-1">
                <i class="mdi mdi-phone text-sm text-teal-300" />{{ shop.phone }}
              </span>
              <span v-if="shop?.districtName" class="inline-flex items-center gap-1">
                <i class="mdi mdi-map-marker-outline text-sm text-teal-300" />{{ shop.districtName }}
              </span>
            </div>
          </div>
        </div>

        <div class="flex flex-wrap items-center gap-3">
          <button
            type="button"
            class="inline-flex cursor-pointer items-center gap-2 rounded-2xl border border-white/25 bg-white/10 px-4 py-2.5 text-sm font-bold text-white backdrop-blur-md transition hover:bg-white/20 active:scale-95"
            :disabled="loading"
            @click="refreshAll"
          >
            <i class="mdi mdi-refresh text-lg" :class="{ 'animate-spin': loading }" />
            <span>รีเฟรช</span>
          </button>

          <a
            v-if="shopId"
            :href="shopPublicUrl"
            target="_blank"
            rel="noopener noreferrer"
            class="inline-flex items-center gap-2 rounded-2xl bg-teal-400 px-5 py-2.5 text-sm font-black text-slate-900 shadow-md transition hover:bg-teal-300 active:scale-95"
          >
            <i class="mdi mdi-open-in-new text-lg" />
            <span>ดูหน้าร้านจริง</span>
          </a>
        </div>
      </div>
    </section>

    <!-- KPI STAT CARDS (คลิกเพื่อกรอง) -->
    <section class="grid grid-cols-2 gap-4 lg:grid-cols-4">
      <button
        v-for="card in kpiCards"
        :key="card.key"
        type="button"
        class="group relative overflow-hidden rounded-2xl border bg-white p-4 text-left shadow-xs transition"
        :class="[
          card.tab ? 'cursor-pointer hover:-translate-y-0.5 hover:shadow-md' : 'cursor-default',
          card.tab && activeTab === card.tab ? [card.activeBorder, 'ring-4', card.ring] : 'border-slate-300',
        ]"
        @click="card.tab && selectTab(card.tab)"
      >
        <span class="absolute inset-y-0 left-0 w-1.5" :class="card.bar" />
        <div class="flex items-start justify-between gap-2 pl-1.5">
          <div class="min-w-0">
            <p class="truncate text-xs font-black tracking-wide text-slate-600">{{ card.label }}</p>
            <p class="mt-1.5 text-3xl font-black leading-none" :class="card.text">
              {{ card.value.toLocaleString('th-TH') }}
            </p>
          </div>
          <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-xl" :class="card.iconBox">
            <i :class="['mdi', card.icon]" />
          </div>
        </div>
        <p class="mt-3 truncate pl-1.5 text-xs font-bold text-slate-500">{{ card.hint }}</p>
      </button>
    </section>

    <!-- SEARCH & FILTER -->
    <section class="rounded-2xl border border-slate-300 bg-white p-3 shadow-xs sm:p-4">
      <div class="flex flex-col gap-3 xl:flex-row xl:items-center xl:justify-between">
        <div class="flex gap-1.5 overflow-x-auto rounded-2xl bg-slate-100 p-1.5">
          <button
            v-for="tab in statusTabs"
            :key="tab.value"
            type="button"
            class="inline-flex shrink-0 cursor-pointer items-center gap-2 rounded-xl px-3.5 py-2 text-xs font-bold transition sm:text-sm"
            :class="
              activeTab === tab.value
                ? 'bg-white text-teal-800 shadow-sm ring-1 ring-slate-300'
                : 'text-slate-600 hover:bg-white/60 hover:text-slate-900'
            "
            @click="selectTab(tab.value as any)"
          >
            <i :class="['mdi text-base', tab.icon]" />
            <span>{{ tab.label }}</span>
            <span
              class="rounded-full px-2 py-0.5 text-xs font-black"
              :class="activeTab === tab.value ? 'bg-teal-600 text-white' : 'bg-slate-200 text-slate-700'"
            >
              {{
                tab.value === 'All'
                  ? stats.all
                  : tab.value === 'Active'
                    ? stats.active
                    : stats.inactive
              }}
            </span>
          </button>
        </div>

        <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
          <select
            v-model="selectedCategoryId"
            class="w-full cursor-pointer rounded-2xl border border-slate-300 bg-slate-50 px-4 py-2.5 text-sm font-bold text-slate-800 focus:border-teal-600 focus:bg-white focus:outline-none focus:ring-4 focus:ring-teal-600/15 sm:w-56"
          >
            <option value="">ทุกหมวดหมู่สินค้า</option>
            <option v-for="cat in categories" :key="cat.productCategoryId" :value="cat.productCategoryId">
              {{ cat.categoryName }}
            </option>
          </select>

          <div class="relative w-full sm:w-80">
            <i class="mdi mdi-magnify absolute left-4 top-1/2 -translate-y-1/2 text-xl text-slate-500" />
            <input
              v-model="search"
              type="text"
              placeholder="ค้นหาชื่อสินค้า รหัส หรือรายละเอียด..."
              class="w-full rounded-2xl border border-slate-300 bg-slate-50 py-2.5 pl-11 pr-10 text-sm font-bold text-slate-900 placeholder:font-bold placeholder:text-slate-500 focus:border-teal-600 focus:bg-white focus:outline-none focus:ring-4 focus:ring-teal-600/15"
            />
            <button
              v-if="search"
              type="button"
              class="absolute right-3 top-1/2 -translate-y-1/2 cursor-pointer text-slate-500 hover:text-slate-800"
              title="ล้างคำค้นหา"
              @click="search = ''"
            >
              <i class="mdi mdi-close-circle text-lg" />
            </button>
          </div>

          <button
            v-if="search || selectedCategoryId"
            type="button"
            class="cursor-pointer whitespace-nowrap rounded-xl px-3 py-2 text-sm font-bold text-teal-800 underline underline-offset-2 hover:bg-teal-50"
            @click="clearFilters"
          >
            ล้างตัวกรอง
          </button>
        </div>
      </div>
    </section>

    <!-- DATA TABLE -->
    <AppDataTable
      :columns="columns"
      :items="displayProducts"
      row-key="productId"
      :loading="loading"
      :pagination="pagination"
      :variant="'admin'"
      empty-message="ไม่พบข้อมูลสินค้าตามเงื่อนไขที่เลือก"
      @page-change="changePage"
    >
      <!-- PRODUCT INFO COLUMN -->
      <template #cell-product="{ item }">
        <div class="flex items-center gap-3.5 py-1.5">
          <div
            class="flex h-14 w-14 shrink-0 items-center justify-center overflow-hidden rounded-2xl border border-slate-300 bg-gradient-to-br from-teal-50 to-slate-100 text-teal-700 shadow-xs"
          >
            <img
              v-if="asProduct(item).imageUrl"
              :src="asProduct(item).imageUrl"
              :alt="asProduct(item).productName"
              class="h-full w-full object-cover"
            />
            <i v-else class="mdi mdi-package-variant text-2xl" />
          </div>

          <div class="min-w-0">
            <p
              class="cursor-pointer truncate text-sm font-black text-slate-900 transition hover:text-teal-700"
              @click="openProductDetail(asProduct(item))"
            >
              {{ asProduct(item).productName }}
            </p>
            <div class="mt-1 flex flex-wrap items-center gap-2">
              <span class="font-mono text-xs font-bold text-slate-500">
                #{{ asProduct(item).productId.slice(0, 8) }}
              </span>
              <span
                v-if="asProduct(item).activeDeal"
                class="inline-flex items-center gap-1 rounded-lg border border-amber-300 bg-amber-100 px-2 py-0.5 text-xs font-black text-amber-800"
              >
                <i class="mdi mdi-fire text-sm" /> มีโปรโมชัน
              </span>
            </div>
          </div>
        </div>
      </template>

      <!-- CATEGORY COLUMN -->
      <template #cell-category="{ item }">
        <span
          class="inline-flex items-center gap-1 rounded-lg border border-teal-200 bg-teal-50 px-2.5 py-1 text-xs font-bold text-teal-800"
        >
          <i class="mdi mdi-tag-outline text-sm text-teal-600" />
          {{ getCategoryName(asProduct(item).productCategoryId) }}
        </span>
      </template>

      <!-- PRICE COLUMN -->
      <template #cell-price="{ item }">
        <div>
          <span class="text-base font-black text-slate-900">
            {{ formatCurrency(asProduct(item).price) }}
          </span>
          <span
            v-if="asProduct(item).activeDeal"
            class="block text-xs font-black text-amber-700"
          >
            ลดพิเศษ
          </span>
        </div>
      </template>

      <!-- STOCK COLUMN -->
      <template #cell-quantity="{ item }">
        <span
          class="inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-black"
          :class="stockStatus(asProduct(item).quantity).badgeClass"
        >
          <span class="h-2 w-2 rounded-full" :class="stockStatus(asProduct(item).quantity).dotClass" />
          {{ stockStatus(asProduct(item).quantity).text }}
        </span>
      </template>

      <!-- STATUS BADGE COLUMN -->
      <template #cell-status="{ item }">
        <span
          class="inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-black"
          :class="
            asProduct(item).status === 'Active'
              ? 'border-emerald-300 bg-emerald-100 text-emerald-800'
              : 'border-slate-400 bg-slate-200 text-slate-800'
          "
        >
          <span
            class="h-2 w-2 rounded-full"
            :class="asProduct(item).status === 'Active' ? 'bg-emerald-600' : 'bg-slate-500'"
          />
          {{ asProduct(item).status === 'Active' ? 'เปิดขายปกติ' : 'ปิดการขาย' }}
        </span>
      </template>

      <!-- ACTIONS COLUMN -->
      <template #cell-actions="{ item }">
        <div class="flex items-center justify-end gap-2">
          <button
            type="button"
            class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-slate-300 bg-white px-3 py-2 text-xs font-bold text-slate-700 shadow-xs transition hover:border-teal-600 hover:bg-teal-50 hover:text-teal-800"
            title="ดูรายละเอียดสินค้า"
            @click="openProductDetail(asProduct(item))"
          >
            <i class="mdi mdi-eye-outline text-base" />
            <span>ดูข้อมูล</span>
          </button>

          <button
            v-if="asProduct(item).status === 'Active'"
            type="button"
            class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-rose-300 bg-rose-50 px-3 py-2 text-xs font-bold text-rose-800 transition hover:bg-rose-100 disabled:opacity-50"
            :disabled="updatingId === asProduct(item).productId"
            title="ปิดการขายสินค้าชั่วคราว"
            @click="toggleProductStatus(asProduct(item))"
          >
            <i
              class="mdi text-base"
              :class="updatingId === asProduct(item).productId ? 'mdi-loading animate-spin' : 'mdi-pause-circle-outline'"
            />
            <span>ปิดการขาย</span>
          </button>

          <button
            v-else
            type="button"
            class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl bg-emerald-700 px-3 py-2 text-xs font-bold text-white shadow-xs transition hover:bg-emerald-800 active:scale-95 disabled:opacity-50"
            :disabled="updatingId === asProduct(item).productId"
            title="เปิดการขายสินค้า"
            @click="toggleProductStatus(asProduct(item))"
          >
            <i
              class="mdi text-base"
              :class="updatingId === asProduct(item).productId ? 'mdi-loading animate-spin' : 'mdi-check-circle-outline'"
            />
            <span>เปิดขาย</span>
          </button>
        </div>
      </template>
    </AppDataTable>

    <!-- PRODUCT DETAIL INSPECTION MODAL -->
    <Teleport to="body">
      <div
        v-if="selectedProduct"
        class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/70 p-4 backdrop-blur-sm"
        @click.self="closeProductDetail"
      >
        <div
          class="relative max-h-[92vh] w-full max-w-3xl overflow-y-auto rounded-3xl border border-slate-300 bg-white shadow-2xl"
        >
          <!-- MODAL HEADER -->
          <div class="relative bg-gradient-to-br from-slate-900 via-slate-800 to-teal-900 p-6 text-white">
            <button
              type="button"
              class="absolute right-4 top-4 flex h-9 w-9 cursor-pointer items-center justify-center rounded-xl bg-white/10 text-white transition hover:bg-white/25"
              title="ปิด"
              @click="closeProductDetail"
            >
              <i class="mdi mdi-close text-xl" />
            </button>
            <p class="flex items-center gap-1.5 text-xs font-black text-teal-300">
              <i class="mdi mdi-cube-outline text-base" /> ข้อมูลรายละเอียดสินค้า
            </p>
            <h3 class="mt-1 pr-10 text-xl font-black sm:text-2xl">
              {{ selectedProduct.productName }}
            </h3>
            <p class="mt-1 text-xs font-bold text-slate-300">
              ร้านค้า: {{ shop?.shopName || selectedProduct.shopName || shopId }}
            </p>
          </div>

          <!-- MODAL BODY -->
          <div class="grid grid-cols-1 gap-6 p-6 md:grid-cols-2">
            <!-- IMAGE GALLERY PREVIEW -->
            <div class="space-y-3">
              <div
                class="flex aspect-square w-full items-center justify-center overflow-hidden rounded-2xl border border-slate-300 bg-slate-100"
              >
                <img
                  v-if="activePreviewImage"
                  :src="activePreviewImage"
                  :alt="selectedProduct.productName"
                  class="h-full w-full object-cover"
                />
                <div v-else class="p-6 text-center text-slate-500">
                  <i class="mdi mdi-image-off-outline mb-2 text-5xl" />
                  <p class="text-sm font-bold">ไม่มีรูปภาพสินค้า</p>
                </div>
              </div>

              <div
                v-if="selectedProduct.detailImages && selectedProduct.detailImages.length > 0"
                class="flex items-center gap-2 overflow-x-auto pb-1"
              >
                <button
                  v-if="selectedProduct.imageUrl"
                  type="button"
                  class="h-16 w-16 shrink-0 cursor-pointer overflow-hidden rounded-xl border-2 transition"
                  :class="activePreviewImage === selectedProduct.imageUrl ? 'border-teal-600 ring-4 ring-teal-600/20' : 'border-slate-300 opacity-70 hover:opacity-100'"
                  @click="activePreviewImage = selectedProduct.imageUrl!"
                >
                  <img :src="selectedProduct.imageUrl" class="h-full w-full object-cover" />
                </button>

                <button
                  v-for="(img, idx) in selectedProduct.detailImages"
                  :key="idx"
                  type="button"
                  class="h-16 w-16 shrink-0 cursor-pointer overflow-hidden rounded-xl border-2 transition"
                  :class="activePreviewImage === img ? 'border-teal-600 ring-4 ring-teal-600/20' : 'border-slate-300 opacity-70 hover:opacity-100'"
                  @click="activePreviewImage = img"
                >
                  <img :src="img" class="h-full w-full object-cover" />
                </button>
              </div>
            </div>

            <!-- PRODUCT INFORMATION -->
            <div class="space-y-4">
              <div class="space-y-3 rounded-2xl border border-teal-200 bg-teal-50/60 p-4">
                <div class="flex items-center justify-between">
                  <span class="text-xs font-black text-slate-600">สถานะสินค้า</span>
                  <span
                    class="inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-black"
                    :class="
                      selectedProduct.status === 'Active'
                        ? 'border-emerald-300 bg-emerald-100 text-emerald-800'
                        : 'border-slate-400 bg-slate-200 text-slate-800'
                    "
                  >
                    <span
                      class="h-2 w-2 rounded-full"
                      :class="selectedProduct.status === 'Active' ? 'bg-emerald-600' : 'bg-slate-500'"
                    />
                    {{ selectedProduct.status === 'Active' ? 'เปิดขายปกติ' : 'ปิดการขาย' }}
                  </span>
                </div>

                <div>
                  <span class="text-xs font-black text-slate-600">ราคาขาย</span>
                  <p class="text-3xl font-black text-teal-800">
                    {{ formatCurrency(selectedProduct.price) }}
                  </p>
                </div>

                <div class="flex items-center justify-between border-t border-teal-200 pt-3 text-xs">
                  <span class="font-black text-slate-600">สต็อกคงเหลือ</span>
                  <span
                    class="inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 font-black"
                    :class="stockStatus(selectedProduct.quantity).badgeClass"
                  >
                    {{ stockStatus(selectedProduct.quantity).text }}
                  </span>
                </div>
              </div>

              <div class="space-y-1 text-xs">
                <div class="flex items-center justify-between gap-3 border-b border-slate-200 py-2">
                  <span class="font-bold text-slate-600">หมวดหมู่</span>
                  <span class="font-black text-slate-900">
                    {{ getCategoryName(selectedProduct.productCategoryId) }}
                  </span>
                </div>

                <div class="flex items-center justify-between gap-3 border-b border-slate-200 py-2">
                  <span class="shrink-0 font-bold text-slate-600">รหัสสินค้า</span>
                  <span class="select-all break-all text-right font-mono font-bold text-slate-700">
                    {{ selectedProduct.productId }}
                  </span>
                </div>

                <div class="flex items-center justify-between gap-3 border-b border-slate-200 py-2">
                  <span class="shrink-0 font-bold text-slate-600">รหัสร้านค้า</span>
                  <span class="select-all break-all text-right font-mono font-bold text-slate-700">
                    {{ selectedProduct.shopId }}
                  </span>
                </div>

                <div
                  v-if="(selectedProduct as any).createdAt"
                  class="flex items-center justify-between gap-3 border-b border-slate-200 py-2"
                >
                  <span class="font-bold text-slate-600">สร้างเมื่อ</span>
                  <span class="font-bold text-slate-800">
                    {{ formatDate((selectedProduct as any).createdAt) }}
                  </span>
                </div>

                <div
                  v-if="(selectedProduct as any).updatedAt"
                  class="flex items-center justify-between gap-3 border-b border-slate-200 py-2"
                >
                  <span class="font-bold text-slate-600">อัปเดตล่าสุด</span>
                  <span class="font-bold text-slate-800">
                    {{ formatDate((selectedProduct as any).updatedAt) }}
                  </span>
                </div>
              </div>

              <div>
                <p class="mb-1.5 flex items-center gap-1.5 text-xs font-black text-slate-600">
                  <i class="mdi mdi-text-box-outline text-base text-teal-700" /> รายละเอียดสินค้า
                </p>
                <div
                  class="max-h-36 overflow-y-auto whitespace-pre-line rounded-2xl border border-slate-300 bg-slate-50 p-3 text-sm font-bold leading-relaxed text-slate-700"
                >
                  {{ selectedProduct.description || 'ไม่มีคำอธิบายสำหรับสินค้านี้' }}
                </div>
              </div>
            </div>
          </div>

          <!-- MODAL FOOTER -->
          <div
            class="sticky bottom-0 flex flex-wrap items-center justify-between gap-3 border-t border-slate-300 bg-white/95 px-6 py-4 backdrop-blur"
          >
            <div>
              <button
                v-if="selectedProduct.status === 'Active'"
                type="button"
                class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-rose-300 bg-rose-50 px-4 py-2.5 text-sm font-bold text-rose-800 transition hover:bg-rose-100 disabled:opacity-50"
                :disabled="updatingId === selectedProduct.productId"
                @click="toggleProductStatus(selectedProduct)"
              >
                <i class="mdi mdi-pause-circle-outline text-lg" />
                <span>ปิดการขายสินค้านี้ชั่วคราว</span>
              </button>

              <button
                v-else
                type="button"
                class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl bg-emerald-700 px-4 py-2.5 text-sm font-bold text-white shadow-xs transition hover:bg-emerald-800 active:scale-95 disabled:opacity-50"
                :disabled="updatingId === selectedProduct.productId"
                @click="toggleProductStatus(selectedProduct)"
              >
                <i class="mdi mdi-check-circle-outline text-lg" />
                <span>เปิดการขายสินค้านี้</span>
              </button>
            </div>

            <button
              type="button"
              class="cursor-pointer rounded-xl border border-slate-300 bg-white px-5 py-2.5 text-sm font-bold text-slate-700 transition hover:bg-slate-100"
              @click="closeProductDetail"
            >
              ปิดหน้าต่าง
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </main>
</template>
