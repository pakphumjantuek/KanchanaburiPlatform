<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import {
  getDistricts,
  getPublicProducts,
  getShopCategories,
  getShops,
  type District,
  type Product,
  type Shop,
  type ShopCategory,
} from '@/features/shops/api'

// Sub-components
import ShopListSidebarFilter from '../components/ShopListSidebarFilter.vue'
import ShopCardItem from '../components/ShopCardItem.vue'
import ShopMerchantCtaBanner from '../components/ShopMerchantCtaBanner.vue'
import ShopMapView from '@/features/shops/components/ShopMapView.vue'
import ShopMapTopFilter from '../components/ShopMapTopFilter.vue'

import AppPagination from '@/shared/components/AppPagination.vue'
import AppEmptyState from '@/shared/components/AppEmptyState.vue'
import { vReveal } from '@/shared/directives/reveal'
import { usePagination } from '@/shared/composables/usePagination'

const route = useRoute()

const shops = ref<Shop[]>([])
const categories = ref<ShopCategory[]>([])
const districts = ref<District[]>([])
const allProducts = ref<Product[]>([])

const viewMode = ref<'shops' | 'map'>('shops')

const productsByShopId = computed(() => {
  const map = new Map<string, Product[]>()
  for (const prod of allProducts.value) {
    if (!map.has(prod.shopId)) {
      map.set(prod.shopId, [])
    }
    map.get(prod.shopId)!.push(prod)
  }
  return map
})

const { page, pageSize, totalCount, totalPages, setPageResult, resetPage } = usePagination(12)

const search = ref('')
const categoryId = ref<string | null>(null)
const districtId = ref<string | null>(null)
const verifiedOnly = ref(false)
const sortBy = ref<'latest' | 'popular' | 'title'>('latest')
const showMobileFilter = ref(false)

const loading = ref(true)

const mapShops = ref<Shop[]>([])
const mapLoading = ref(false)

const filteredMapShops = computed(() => {
  let list = mapShops.value
  if (verifiedOnly.value) {
    list = list.filter((s) => s.status === 'Active')
  }
  return list
})

// The shops API has no status filter, so "verified only" is applied to the loaded page
const visibleShops = computed(() =>
  verifiedOnly.value ? shops.value.filter((s) => s.status === 'Active') : shops.value,
)

type ChipKey = 'search' | 'category' | 'district' | 'verified'

const activeChips = computed(() => {
  const chips: { key: ChipKey; label: string }[] = []
  if (search.value.trim()) chips.push({ key: 'search', label: `ค้นหา “${search.value.trim()}”` })
  const cat = categories.value.find((c) => c.shopCategoryId === categoryId.value)
  if (categoryId.value && cat) chips.push({ key: 'category', label: cat.categoryName })
  const dist = districts.value.find((d) => d.districtId === districtId.value)
  if (districtId.value && dist) chips.push({ key: 'district', label: `อ.${dist.districtName}` })
  if (verifiedOnly.value) chips.push({ key: 'verified', label: 'ผ่านการรับรอง' })
  return chips
})

function removeChip(key: ChipKey) {
  // category / district / verified reload through the watcher below
  if (key === 'category') categoryId.value = null
  else if (key === 'district') districtId.value = null
  else if (key === 'verified') verifiedOnly.value = false
  else {
    search.value = ''
    resetPage()
    load()
  }
}

const sortChoices: { value: 'latest' | 'popular' | 'title'; label: string }[] = [
  { value: 'latest', label: 'มาใหม่' },
  { value: 'popular', label: 'ยอดนิยม' },
  { value: 'title', label: 'ชื่อ A-Z' },
]

const activeFilterCount = computed(() => {
  let count = 0
  if (categoryId.value) count++
  if (districtId.value) count++
  if (search.value.trim()) count++
  if (verifiedOnly.value) count++
  return count
})

async function loadMasterData() {
  try {
    const [catRes, distRes, prodRes] = await Promise.all([
      getShopCategories(),
      getDistricts(),
      getPublicProducts().catch(() => [] as Product[]),
    ])
    categories.value = catRes
    districts.value = distRes
    allProducts.value = prodRes || []
  } catch (err) {
    console.error('Failed loading master data', err)
  }
}

async function loadShopsData() {
  loading.value = true
  try {
    const res = await getShops({
      search: search.value.trim() || undefined,
      categoryId: categoryId.value || undefined,
      districtId: districtId.value || undefined,
      sortBy: sortBy.value,
      page: page.value,
      pageSize: pageSize.value,
    })
    shops.value = res.items || []
    setPageResult(res.totalCount || 0, res.totalPages || 1)
  } catch (err) {
    console.error('Failed loading shops', err)
    shops.value = []
    setPageResult(0, 1)
  } finally {
    loading.value = false
  }
}

async function loadMapShops() {
  mapLoading.value = true
  try {
    const res = await getShops({
      search: search.value.trim() || undefined,
      categoryId: categoryId.value || undefined,
      districtId: districtId.value || undefined,
      sortBy: sortBy.value,
      page: 1,
      pageSize: 500, // Load all shops across Kanchanaburi for map display without pagination cutoff
    })
    mapShops.value = res.items || []
  } catch (err) {
    console.error('Failed loading map shops', err)
    mapShops.value = []
  } finally {
    mapLoading.value = false
  }
}

function load() {
  void loadShopsData()
}

function selectCategoryPill(id: string | null) {
  categoryId.value = id
  if (viewMode.value === 'map') {
    void loadMapShops()
  } else {
    resetPage()
    load()
  }
}

function clearFilters() {
  categoryId.value = null
  districtId.value = null
  search.value = ''
  verifiedOnly.value = false
  sortBy.value = 'latest'
  if (viewMode.value === 'map') {
    void loadMapShops()
  } else {
    resetPage()
    load()
  }
}

function applyQueryParams() {
  if (typeof route.query.categoryId === 'string') categoryId.value = route.query.categoryId
  if (typeof route.query.districtId === 'string') districtId.value = route.query.districtId
  if (typeof route.query.search === 'string') search.value = route.query.search
}

watch([categoryId, districtId, sortBy, verifiedOnly], () => {
  if (viewMode.value === 'map') {
    void loadMapShops()
  } else {
    resetPage()
    load()
  }
})

watch(viewMode, (mode) => {
  if (mode === 'map') {
    void loadMapShops()
  } else {
    resetPage()
    load()
  }
})

watch(
  () => route.query,
  () => {
    applyQueryParams()
    if (viewMode.value === 'map') {
      void loadMapShops()
    } else {
      resetPage()
      load()
    }
  },
)

onMounted(async () => {
  applyQueryParams()
  await loadMasterData()
  if (viewMode.value === 'map') {
    void loadMapShops()
  } else {
    load()
  }
})
</script>

<template>
  <div class="min-h-screen bg-[#F7F0E6] text-[#2C221E] pb-24 font-sans space-y-8">
    <main class="mx-auto mt-5 w-full max-w-[1800px] px-3 sm:px-6 lg:px-10 space-y-6 sm:space-y-8">
      <!-- 1. HERO HEADER BANNER (Modern Warm Marketplace Header) -->
      <section
        class="rounded-[1.75rem] bg-gradient-to-br from-white via-[#FFF9F2] to-[#F7F0E6] border-2 border-[#E8D9C9] p-6 sm:p-8 lg:p-10 shadow-sm relative overflow-hidden space-y-6"
      >
        <div class="pointer-events-none absolute -top-24 -right-16 h-72 w-72 rounded-full bg-[#F2A65A]/25 blur-3xl"></div>
        <div class="pointer-events-none absolute -bottom-28 left-1/3 h-64 w-64 rounded-full bg-[#D96C2C]/15 blur-3xl"></div>
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-6 relative z-10">
          <div class="space-y-2.5 max-w-3xl">
            <div class="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-[#FFF9F2] border-2 border-[#E8D9C9] text-xs font-bold text-[#D96C2C] shadow-2xs">
              <i class="mdi mdi-storefront text-base text-[#D96C2C]"></i>
              <span>ตลาดร้านค้าและผลิตภัณฑ์ชุมชนกาญจนบุรี</span>
            </div>
            <h1 class="text-3xl sm:text-4xl lg:text-5xl font-black text-[#332820] tracking-tight leading-tight">
              ค้นหาร้านค้าและ<span class="text-[#D96C2C]">ของดีเมืองกาญจน์</span>
            </h1>
            <p class="text-sm sm:text-base text-[#786B62] font-semibold leading-relaxed">
              สนับสนุนสินค้าภูมิปัญญาท้องถิ่น งานหัตถกรรม อาหารพื้นบ้าน และผลิตภัณฑ์คุณภาพส่งตรงจากร้านค้า 13 อำเภอ
            </p>
          </div>

          <!-- Quick Stats Badges in Capsules -->
          <div class="flex flex-wrap sm:flex-nowrap items-center gap-3 shrink-0">
            <div class="px-5 py-3.5 rounded-2xl bg-white/80 backdrop-blur border-2 border-[#E8D9C9] text-center min-w-[110px] shadow-sm transition hover:-translate-y-0.5 hover:border-[#D96C2C]">
              <span class="block text-3xl font-black text-[#D96C2C]">{{ totalCount }}</span>
              <span class="text-xs font-bold text-[#786B62]">ร้านค้าทั้งหมด</span>
            </div>
            <div class="px-5 py-3.5 rounded-2xl bg-white/80 backdrop-blur border-2 border-[#E8D9C9] text-center min-w-[110px] shadow-sm transition hover:-translate-y-0.5 hover:border-[#D96C2C]">
              <span class="block text-3xl font-black text-[#D96C2C]">{{ districts.length || '-' }}</span>
              <span class="text-xs font-bold text-[#786B62]">อำเภอในพื้นที่</span>
            </div>
            <div class="px-5 py-3.5 rounded-2xl bg-white/80 backdrop-blur border-2 border-[#E8D9C9] text-center min-w-[110px] shadow-sm transition hover:-translate-y-0.5 hover:border-[#D96C2C]">
              <span class="block text-3xl font-black text-[#D96C2C]">{{ allProducts.length.toLocaleString('th-TH') }}</span>
              <span class="text-xs font-bold text-[#786B62]">สินค้าทั้งหมด</span>
            </div>
          </div>
        </div>

        <!-- QUICK CATEGORY CAPSULE SCROLL STRIP -->
        <div v-if="categories.length" class="pt-4 border-t-2 border-[#E8D9C9] flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
          <button
            type="button"
            class="px-4 py-2 rounded-full text-xs font-black shrink-0 transition border-2 shadow-2xs cursor-pointer"
            :class="
              categoryId === null
                ? 'border-[#D96C2C] bg-[#D96C2C] text-white'
                : 'border-[#E8D9C9] bg-[#FFF9F2] text-[#332820] hover:border-[#D96C2C]'
            "
            @click="selectCategoryPill(null)"
          >
            หมวดหมู่ทั้งหมด
          </button>
          <button
            v-for="cat in categories"
            :key="cat.shopCategoryId"
            type="button"
            class="px-4 py-2 rounded-full text-xs font-black shrink-0 transition border-2 shadow-2xs cursor-pointer"
            :class="
              categoryId === cat.shopCategoryId
                ? 'border-[#D96C2C] bg-[#D96C2C] text-white'
                : 'border-[#E8D9C9] bg-[#FFF9F2] text-[#332820] hover:border-[#D96C2C]'
            "
            @click="selectCategoryPill(cat.shopCategoryId)"
          >
            {{ cat.categoryName }}
          </button>
        </div>
      </section>

      <!-- 2. FULL-WIDTH INTERACTIVE MAP VIEW MODE (Filters On Top & Wide Map) -->
      <div v-if="viewMode === 'map'" class="space-y-6">
        <!-- TOP HORIZONTAL FILTER BAR FOR MAP -->
        <ShopMapTopFilter
          v-model:search="search"
          v-model:category-id="categoryId"
          v-model:district-id="districtId"
          v-model:verified-only="verifiedOnly"
          v-model:view-mode="viewMode"
          :categories="categories"
          :districts="districts"
          :active-filter-count="activeFilterCount"
          :total-count="filteredMapShops.length"
          @clear-filters="clearFilters"
          @filter-change="loadMapShops"
        />
        <!-- FULL-WIDTH LEAFLET MAP CONTAINER -->
        <ShopMapView :shops="filteredMapShops" :loading="mapLoading" />
      </div>

      <!-- 3. MAIN 2-COLUMN MARKETPLACE LAYOUT (SHOPS LIST MODE) -->
      <template v-else>
        <!-- MOBILE FILTER TOGGLE BAR (Visible on mobile screens < lg) -->
        <div
          class="lg:hidden flex items-center justify-between gap-3 bg-white p-3.5 rounded-[1.75rem] border-2 border-[#E8D9C9] shadow-sm"
        >
          <button
            type="button"
            class="flex-1 flex items-center justify-center gap-2 py-2.5 px-4 rounded-full bg-[#D96C2C] hover:bg-[#BF5720] text-white font-black text-xs sm:text-sm shadow-xs cursor-pointer active:scale-95 transition"
            @click="showMobileFilter = true"
          >
            <i class="mdi mdi-tune-variant text-base"></i>
            <span>ค้นหา & ตัวกรองร้านค้า</span>
            <span
              v-if="activeFilterCount"
              class="px-2 py-0.5 rounded-full bg-white text-[#D96C2C] text-[10px] font-black"
            >
              {{ activeFilterCount }}
            </span>
          </button>

          <button
            v-if="activeFilterCount"
            type="button"
            class="py-2.5 px-4 rounded-full border-2 border-[#E8D9C9] bg-[#FFF9F2] text-xs font-black text-[#332820] hover:text-[#D96C2C] hover:border-[#D96C2C] cursor-pointer active:scale-95 transition shrink-0 shadow-2xs"
            @click="clearFilters"
          >
            ล้างค่า
          </button>
        </div>

        <!-- MOBILE SLIDE-OVER FILTER DRAWER -->
        <Teleport to="body">
          <div
            v-if="showMobileFilter"
            class="fixed inset-0 z-50 flex justify-end bg-black/60 backdrop-blur-xs transition-opacity lg:hidden"
            @click.self="showMobileFilter = false"
          >
            <div
              class="w-full max-w-xs sm:max-w-sm bg-[#FFF9F2] h-full shadow-2xl flex flex-col justify-between p-5 space-y-4 overflow-y-auto animate-in slide-in-from-right duration-300"
            >
              <div class="space-y-4">
                <div class="flex items-center justify-between border-b-2 border-[#E8D9C9] pb-3">
                  <h3 class="font-black text-[#332820] text-lg flex items-center gap-2">
                    <i class="mdi mdi-tune-variant text-[#D96C2C] text-xl"></i>
                    <span>ตัวกรองค้นหาร้านค้า</span>
                  </h3>
                  <button
                    type="button"
                    class="h-9 w-9 rounded-full bg-white text-[#332820] flex items-center justify-center cursor-pointer border-2 border-[#E8D9C9]"
                    @click="showMobileFilter = false"
                  >
                    <i class="mdi mdi-close text-lg"></i>
                  </button>
                </div>

                <ShopListSidebarFilter
                  v-model:search="search"
                  v-model:category-id="categoryId"
                  v-model:district-id="districtId"
                  v-model:verified-only="verifiedOnly"
                  :categories="categories"
                  :districts="districts"
                  :active-filter-count="activeFilterCount"
                  @clear-filters="clearFilters"
                  @filter-change="
                    () => {
                      resetPage()
                      load()
                    }
                  "
                />
              </div>

              <div
                class="pt-4 border-t-2 border-[#E8D9C9] space-y-2 sticky bottom-0 bg-[#FFF9F2] pt-3"
              >
                <button
                  type="button"
                  class="w-full py-3 rounded-full bg-[#D96C2C] hover:bg-[#BF5720] text-white font-black text-xs sm:text-sm shadow-md transition cursor-pointer"
                  @click="showMobileFilter = false"
                >
                  แสดงผลร้านค้า ({{ totalCount }})
                </button>
              </div>
            </div>
          </div>
        </Teleport>

        <div class="grid grid-cols-1 lg:grid-cols-12 gap-4 sm:gap-6 lg:gap-8 items-start">
          <!-- LEFT SIDEBAR FILTER PANEL (Persistent on Desktop) -->
          <div class="hidden lg:block lg:col-span-3 lg:sticky lg:top-24">
            <ShopListSidebarFilter
              v-model:search="search"
              v-model:category-id="categoryId"
              v-model:district-id="districtId"
              v-model:verified-only="verifiedOnly"
              :categories="categories"
              :districts="districts"
              :active-filter-count="activeFilterCount"
              @clear-filters="clearFilters"
              @filter-change="
                () => {
                  resetPage()
                  load()
                }
              "
            />
          </div>

          <!-- RIGHT STORE CARDS GRID CONTENT -->
          <div class="lg:col-span-9 space-y-6">
            <!-- ACTIVE FILTER CHIPS -->
            <div v-if="activeChips.length" class="flex flex-wrap items-center gap-2">
              <span class="text-xs font-bold text-[#786B62]">กำลังกรอง:</span>
              <button
                v-for="chip in activeChips"
                :key="chip.key"
                type="button"
                class="flex cursor-pointer select-none items-center gap-1.5 rounded-full border border-[#D96C2C]/40 bg-white px-3 py-1 text-xs font-bold text-[#332820] shadow-2xs transition hover:border-[#D96C2C] hover:bg-[#FFF9F2] active:scale-95 focus-visible:ring-2 focus-visible:ring-[#D96C2C]/40 focus-visible:outline-none"
                @click="removeChip(chip.key)"
              >
                <span>{{ chip.label }}</span>
                <i class="mdi mdi-close text-sm text-[#D96C2C]"></i>
              </button>
              <button
                type="button"
                class="cursor-pointer text-xs font-bold text-[#D96C2C] underline-offset-2 hover:underline"
                @click="clearFilters"
              >
                ล้างทั้งหมด
              </button>
            </div>

            <!-- TOOLBAR: TOTALS & VIEW MODE TOGGLE & SORTING -->
            <div
              class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 rounded-[1.75rem] bg-white p-4 sm:p-5 border-2 border-[#E8D9C9] shadow-md shadow-[#D96C2C]/5"
            >
              <div>
                <h2 class="text-lg sm:text-xl font-black text-[#332820] tracking-tight">
                  รายการร้านค้าชุมชน
                </h2>
                <p class="text-xs sm:text-sm text-[#786B62] font-semibold mt-0.5">
                  <template v-if="verifiedOnly">
                    แสดง <span class="text-[#D96C2C] font-black">{{ visibleShops.length }}</span> จาก
                    {{ shops.length }} ร้านค้าในหน้านี้ (เฉพาะที่ผ่านการรับรอง)
                  </template>
                  <template v-else>
                    พบ <span class="text-[#D96C2C] font-black">{{ totalCount }}</span> ร้านค้าในระบบ
                  </template>
                </p>
              </div>

              <!-- View Mode Switcher & Sort By Dropdown -->
              <div
                class="flex items-center gap-3 shrink-0 flex-wrap sm:flex-nowrap w-full sm:w-auto justify-between sm:justify-end"
              >
                <!-- Mode Switcher Buttons (Capsule) -->
                <div
                  class="inline-flex rounded-full border-2 border-[#E8D9C9] bg-[#FFF9F2] p-1 shadow-2xs"
                >
                  <button
                    type="button"
                    class="flex items-center gap-1.5 px-4 py-1.5 rounded-full text-xs font-black transition cursor-pointer"
                    :class="
                      (viewMode as string) === 'shops'
                        ? 'bg-[#D96C2C] text-white shadow-xs'
                        : 'text-[#786B62] hover:text-[#332820]'
                    "
                    @click="viewMode = 'shops'"
                  >
                    <i class="mdi mdi-storefront-outline text-base"></i>
                    <span>ร้านค้า</span>
                  </button>
                  <button
                    type="button"
                    class="flex items-center gap-1.5 px-4 py-1.5 rounded-full text-xs font-black transition cursor-pointer"
                    :class="
                      (viewMode as string) === 'map'
                        ? 'bg-[#D96C2C] text-white shadow-xs'
                        : 'text-[#786B62] hover:text-[#332820]'
                    "
                    @click="viewMode = 'map'"
                  >
                    <i class="mdi mdi-map-marker-radius text-base"></i>
                    <span>แผนที่</span>
                  </button>
                </div>

                <!-- Sort (chips) -->
                <div class="flex items-center gap-1.5" role="radiogroup" aria-label="เรียงลำดับร้านค้า">
                  <span class="mr-0.5 hidden text-xs font-bold text-[#786B62] sm:inline">เรียง:</span>
                  <button
                    v-for="opt in sortChoices"
                    :key="opt.value"
                    type="button"
                    role="radio"
                    :aria-checked="sortBy === opt.value"
                    class="cursor-pointer select-none rounded-full border-2 px-3.5 py-1.5 text-xs font-black transition focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none active:scale-95"
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
            </div>

            <!-- SKELETON LOADING -->
            <div v-if="loading" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              <div
                v-for="i in 12"
                :key="i"
                class="animate-pulse overflow-hidden rounded-[1.75rem] bg-white border-2 border-[#E8D9C9] h-96 shadow-xs"
              >
                <div class="aspect-[16/9] bg-[#F7F0E6]"></div>
                <div class="p-5 space-y-4">
                  <div class="flex items-center gap-3.5">
                    <div class="h-14 w-14 rounded-2xl bg-[#E8D9C9]"></div>
                    <div class="flex-1 space-y-2">
                      <div class="h-4 bg-[#E8D9C9] rounded-full w-3/4"></div>
                      <div class="h-3 bg-[#F7F0E6] rounded-full w-1/2"></div>
                    </div>
                  </div>
                  <div class="h-10 bg-[#F7F0E6] rounded-2xl"></div>
                </div>
              </div>
            </div>

            <!-- SHOPS GRID (Displays Real Products inside each shop card) -->
            <div
              v-else-if="visibleShops.length"
              class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5 sm:gap-6"
            >
              <ShopCardItem
                v-for="(shop, idx) in visibleShops"
                :key="shop.shopId"
                v-reveal
                :style="{ animationDelay: `${(idx % 3) * 90}ms` }"
                :shop="shop"
                :products="productsByShopId.get(shop.shopId) || []"
              />
            </div>

            <!-- EMPTY STATE -->
            <AppEmptyState
              v-else
              icon="mdi-store-off-outline"
              title="ไม่พบร้านค้าตามเงื่อนไข"
              description="ลองปรับเปลี่ยนคำค้นหา หรือเลือกตัวกรองอำเภอและประเภทร้านค้าอื่น"
              action-label="ล้างตัวกรองทั้งหมด"
              @action="clearFilters"
            />

            <!-- PAGINATION -->
            <AppPagination v-model:page="page" :total-pages="totalPages" @change="load" />
          </div>
        </div>
      </template>

      <!-- 4. MERCHANT REGISTRATION CTA BANNER -->
      <ShopMerchantCtaBanner />
    </main>
  </div>
</template>
