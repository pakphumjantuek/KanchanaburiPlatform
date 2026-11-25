<template>
  <div class="min-h-screen bg-gradient-to-b from-[#F7F0E6] via-[#F7F0E6] to-[#FFF9F2] pb-20 text-[#332820]">
    <!-- HERO BANNER SECTION -->
    <ContentHeroBanner v-model:search="search" v-model:viewMode="viewMode" @search="onSearch" />

    <!-- CATEGORY BAR NAV -->
    <ContentCategoryBar
      :categories="categories"
      :selectedCategoryId="categoryId"
      @select="selectCategoryPill"
    />

    <main class="mx-auto w-full max-w-[1800px] space-y-5 px-4 py-6 sm:space-y-6 sm:px-6 sm:py-8 lg:px-10">
      <!-- FILTER SECTION -->
      <ContentFilterSection
        v-model:categoryId="categoryId"
        v-model:districtId="districtId"
        v-model:subDistrictId="subDistrictId"
        v-model:tagId="tagId"
        v-model:sortBy="sortBy"
        v-model:filtersOpen="filtersOpen"
        :categories="categories"
        :districts="districts"
        :subDistricts="subDistricts"
        :tags="tags"
        :sortOptions="sortOptions"
        :activeFilterCount="activeFilterCount"
        @change-district="changeDistrict"
        @select-tag="selectTag"
        @clear-filters="clearFilters"
        @filter-change="onFilterChange"
      />

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

      <!-- MODE 1: CONTENT CARDS GRID / LIST VIEW -->
      <div v-if="viewMode === 'contents'" class="space-y-6">
        <!-- RESULTS TOOLBAR -->
        <AppDisplayToolbar
          v-model:display-mode="contentDisplayMode"
          :total-count="totalCount"
          unit-label="คอนเทนต์ท่องเที่ยวและวัฒนธรรม"
        />

        <!-- SKELETON LOADING -->
        <div v-if="loading" class="grid grid-cols-1 gap-5 sm:grid-cols-2 sm:gap-6 md:grid-cols-3 xl:grid-cols-4">
          <div
            v-for="i in 6"
            :key="i"
            class="animate-pulse overflow-hidden rounded-3xl border border-[#E8D9C9] bg-white"
          >
            <div class="aspect-16/10 w-full bg-[#F7F0E6]" />
            <div class="space-y-3 p-5">
              <div class="h-4 w-4/5 rounded-full bg-[#E8D9C9]" />
              <div class="h-3 w-full rounded-full bg-[#F7F0E6]" />
              <div class="h-3 w-2/3 rounded-full bg-[#F7F0E6]" />
            </div>
          </div>
        </div>

        <!-- GRID MODE -->
        <div
          v-else-if="contents.length && contentDisplayMode === 'grid'"
          class="grid grid-cols-1 gap-5 sm:grid-cols-2 sm:gap-6 md:grid-cols-3 xl:grid-cols-4"
        >
          <ContentCard
            v-for="(item, index) in contents"
            :key="item.contentId"
            :item="item"
            display-mode="grid"
            v-reveal
            :style="{ animationDelay: `${(index % 3) * 90}ms` }"
          />
        </div>

        <!-- LIST MODE -->
        <div v-else-if="contents.length && contentDisplayMode === 'list'" class="space-y-4">
          <ContentCard
            v-for="(item, index) in contents"
            :key="item.contentId"
            :item="item"
            display-mode="list"
            v-reveal
            :style="{ animationDelay: `${(index % 3) * 90}ms` }"
          />
        </div>

        <!-- EMPTY STATE -->
        <AppEmptyState
          v-else
          icon="mdi-play-box-remove-outline"
          title="ไม่พบข้อมูลคอนเทนต์"
          description="ลองปรับเปลี่ยนคำค้นหาหรือตัวกรองหมวดหมู่ อำเภอ เพื่อค้นหาเรื่องราวที่คุณสนใจ"
          action-label="ล้างตัวกรองทั้งหมด"
          @action="clearFilters"
        />

        <!-- PAGINATION -->
        <AppPagination v-model:page="page" :total-pages="totalPages" @change="load" />
      </div>

      <!-- MODE 2: FULL-DATA MAP VIEW (ALL PINS WITHOUT PAGINATION CUTOFF) -->
      <div v-else-if="viewMode === 'map'">
        <ContentMapView :contents="mapContents" :loading="mapLoading" />
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import {
  getContentCategories,
  getPublicContents,
  getTags,
  type ContentCategory,
  type PublicContent,
  type Tag,
} from '@/features/contents/api'

import ContentHeroBanner from '../components/ContentHeroBanner.vue'
import ContentCategoryBar from '../components/ContentCategoryBar.vue'
import ContentFilterSection from '../components/ContentFilterSection.vue'
import ContentCard from '../components/ContentCard.vue'
import ContentMapView from '../components/ContentMapView.vue'

import AppPagination from '@/shared/components/AppPagination.vue'
import AppEmptyState from '@/shared/components/AppEmptyState.vue'
import AppDisplayToolbar from '@/shared/components/AppDisplayToolbar.vue'
import { vReveal } from '@/shared/directives/reveal'
import { usePagination } from '@/shared/composables/usePagination'
import { useLocations } from '@/shared/composables/useLocations'

const route = useRoute()


const contents = ref<PublicContent[]>([])
const mapContents = ref<PublicContent[]>([])
const categories = ref<ContentCategory[]>([])
const tags = ref<Tag[]>([])

const { districts, subDistricts, fetchDistricts, fetchSubDistricts } = useLocations()
const { page, pageSize, totalCount, totalPages, setPageResult, resetPage } = usePagination(12)

const search = ref('')
const categoryId = ref<string | null>(null)
const districtId = ref<string | null>(null)
const subDistrictId = ref<string | null>(null)
const tagId = ref<string | null>(null)
const sortBy = ref<'latest' | 'popular' | 'title'>('latest')

const viewMode = ref<'contents' | 'map'>('contents')
const contentDisplayMode = ref<'grid' | 'list'>('grid')
const filtersOpen = ref(false)
const loading = ref(true)
const mapLoading = ref(false)

const sortOptions = [
  { id: 'latest', title: 'ล่าสุด' },
  { id: 'popular', title: 'ยอดนิยม' },
  { id: 'title', title: 'ชื่อคอนเทนต์ (A-Z)' },
]

const activeFilterCount = computed(() => {
  let count = 0
  if (categoryId.value) count++
  if (districtId.value) count++
  if (subDistrictId.value) count++
  if (tagId.value) count++
  if (search.value.trim()) count++
  return count
})

type ChipKey = 'search' | 'category' | 'district' | 'subDistrict' | 'tag'

const activeChips = computed(() => {
  const chips: { key: ChipKey; label: string }[] = []
  if (search.value.trim()) chips.push({ key: 'search', label: `ค้นหา “${search.value.trim()}”` })
  const cat = categories.value.find((c) => c.contentCategoryId === categoryId.value)
  if (categoryId.value && cat) chips.push({ key: 'category', label: cat.categoryName })
  const dist = districts.value.find((d) => d.districtId === districtId.value)
  if (districtId.value && dist) chips.push({ key: 'district', label: `อ.${dist.districtName}` })
  const sub = subDistricts.value.find((s) => s.subDistrictId === subDistrictId.value)
  if (subDistrictId.value && sub) chips.push({ key: 'subDistrict', label: `ต.${sub.subDistrictName}` })
  const tg = tags.value.find((t) => t.tagId === tagId.value)
  if (tagId.value && tg) chips.push({ key: 'tag', label: `#${tg.tagName}` })
  return chips
})

function removeChip(key: ChipKey) {
  if (key === 'district') {
    districtId.value = null
    void changeDistrict()
    return
  }
  if (key === 'search') search.value = ''
  if (key === 'category') categoryId.value = null
  if (key === 'subDistrict') subDistrictId.value = null
  if (key === 'tag') tagId.value = null
  onFilterChange()
}

async function loadMasterData() {
  try {
    const [catRes, tagRes] = await Promise.all([getContentCategories(), getTags()])
    categories.value = catRes
    tags.value = tagRes
    await fetchDistricts()
  } catch (err) {
    console.error('Failed loading master data', err)
  }
}

async function loadData() {
  loading.value = true
  try {
    const res = await getPublicContents({
      search: search.value.trim() || undefined,
      categoryId: categoryId.value || undefined,
      districtId: districtId.value || undefined,
      subDistrictId: subDistrictId.value || undefined,
      tagId: tagId.value || undefined,
      sortBy: sortBy.value,
      page: page.value,
      pageSize: pageSize.value,
    })
    contents.value = res.items
    setPageResult(res.totalCount, res.totalPages)
  } catch (err) {
    console.error('Failed loading public contents', err)
    contents.value = []
    setPageResult(0, 1)
  } finally {
    loading.value = false
  }
}

// Load all contents across Kanchanaburi for map display without pagination cutoff
async function loadMapContents() {
  mapLoading.value = true
  try {
    const res = await getPublicContents({
      search: search.value.trim() || undefined,
      categoryId: categoryId.value || undefined,
      districtId: districtId.value || undefined,
      subDistrictId: subDistrictId.value || undefined,
      tagId: tagId.value || undefined,
      sortBy: sortBy.value,
      page: 1,
      pageSize: 500, // Fetch all available contents
    })
    mapContents.value = res.items || []
  } catch (err) {
    console.error('Failed loading map contents', err)
    mapContents.value = []
  } finally {
    mapLoading.value = false
  }
}

function load() {
  void loadData()
}

function onSearch() {
  if (viewMode.value === 'map') {
    void loadMapContents()
  } else {
    resetPage()
    load()
  }
}

function onFilterChange() {
  if (viewMode.value === 'map') {
    void loadMapContents()
  } else {
    resetPage()
    load()
  }
}

async function changeDistrict() {
  subDistrictId.value = null
  await fetchSubDistricts(districtId.value)
  if (viewMode.value === 'map') {
    void loadMapContents()
  } else {
    resetPage()
    load()
  }
}

function selectCategoryPill(id: string | null) {
  categoryId.value = id
  if (viewMode.value === 'map') {
    void loadMapContents()
  } else {
    resetPage()
    load()
  }
}

function selectTag(id: string) {
  tagId.value = tagId.value === id ? null : id
  if (viewMode.value === 'map') {
    void loadMapContents()
  } else {
    resetPage()
    load()
  }
}

function clearFilters() {
  categoryId.value = null
  districtId.value = null
  subDistrictId.value = null
  tagId.value = null
  search.value = ''
  sortBy.value = 'latest'
  if (viewMode.value === 'map') {
    void loadMapContents()
  } else {
    resetPage()
    load()
  }
}

function applyQueryParams() {
  if (route.query.categoryId) categoryId.value = String(route.query.categoryId)
  if (route.query.districtId) districtId.value = String(route.query.districtId)
  if (route.query.tagId) tagId.value = String(route.query.tagId)
  if (route.query.search) search.value = String(route.query.search)
}

watch(viewMode, (mode) => {
  if (mode === 'map') {
    void loadMapContents()
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
      void loadMapContents()
    } else {
      resetPage()
      load()
    }
  },
)

onMounted(async () => {
  applyQueryParams()
  await loadMasterData()
  if (districtId.value) {
    await fetchSubDistricts(districtId.value)
  }
  if (viewMode.value === 'map') {
    void loadMapContents()
  } else {
    load()
  }
})
</script>
