<template>
  <main class="mx-auto w-full max-w-[1920px] px-6 py-8 lg:px-10 space-y-6">
    <!-- HEADER -->
    <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 rounded-3xl border border-slate-300 border-l-8 border-l-teal-600 bg-white p-5 shadow-xs sm:p-6">
      <div>
        <div class="inline-flex items-center gap-2 rounded-lg bg-teal-50 px-3 py-1 text-xs font-bold text-teal-800 mb-2">
          <i class="mdi mdi-folder-multiple-outline text-base" />
          <span>CONTENT CATEGORIES</span>
        </div>
        <h1 class="text-2xl sm:text-3xl font-bold tracking-tight text-slate-900">
          หมวดหมู่คอนเทนต์
        </h1>
        <p class="mt-1 text-sm font-bold text-slate-600">
          จัดการและจัดระเบียบประเภทคอนเทนต์ ข่าวสาร สถานที่ท่องเที่ยว และบทความในระบบ
        </p>
      </div>
      <div class="flex items-center gap-3">
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-bold text-slate-700 shadow-xs hover:bg-slate-50 transition"
          :disabled="loading"
          @click="refreshData"
        >
          <i class="mdi mdi-refresh text-lg" :class="{ 'animate-spin': loading }" />
          <span>รีเฟรช</span>
        </button>
        <RouterLink
          to="/admin/content-categories/new"
          class="inline-flex items-center gap-2 rounded-xl bg-teal-700 px-5 py-2.5 text-sm font-bold text-white shadow-xs hover:bg-teal-800 transition"
        >
          <i class="mdi mdi-plus text-lg" />
          <span>เพิ่มหมวดหมู่ใหม่</span>
        </RouterLink>
      </div>
    </div>

    <!-- STATS KPI CARDS -->
    <section class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <div
        class="cursor-pointer rounded-2xl border bg-white p-5 shadow-xs transition hover:border-teal-300 hover:shadow-sm"
        :class="activeTab === 'All' ? 'border-teal-600 ring-2 ring-teal-600/10' : 'border-slate-300'"
        @click="selectTab('All')"
      >
        <div class="flex items-center justify-between text-slate-600 mb-2">
          <span class="text-xs font-bold uppercase tracking-wider">ทั้งหมด</span>
          <i class="mdi mdi-layers-outline text-xl text-slate-500" />
        </div>
        <p class="text-3xl font-bold text-slate-900">{{ stats.all }}</p>
        <p class="mt-1 text-xs text-slate-600">หมวดหมู่ทั้งหมดในระบบ</p>
      </div>

      <div
        class="cursor-pointer rounded-2xl border bg-white p-5 shadow-xs transition hover:border-emerald-300 hover:shadow-sm"
        :class="activeTab === 'Active' ? 'border-emerald-600 ring-2 ring-emerald-500/10' : 'border-slate-300'"
        @click="selectTab('Active')"
      >
        <div class="flex items-center justify-between text-emerald-600 mb-2">
          <span class="text-xs font-bold uppercase tracking-wider">เปิดใช้งาน (Active)</span>
          <i class="mdi mdi-check-circle-outline text-xl" />
        </div>
        <p class="text-3xl font-bold text-emerald-700">{{ stats.active }}</p>
        <p class="mt-1 text-xs text-slate-600">เปิดให้ผู้ใช้งานและร้านค้าเลือกใช้</p>
      </div>

      <div
        class="cursor-pointer rounded-2xl border bg-white p-5 shadow-xs transition hover:border-slate-400 hover:shadow-sm"
        :class="activeTab === 'Inactive' ? 'border-slate-600 ring-2 ring-slate-500/10' : 'border-slate-300'"
        @click="selectTab('Inactive')"
      >
        <div class="flex items-center justify-between text-slate-600 mb-2">
          <span class="text-xs font-bold uppercase tracking-wider">ปิดใช้งาน (Inactive)</span>
          <i class="mdi mdi-cancel text-xl" />
        </div>
        <p class="text-3xl font-bold text-slate-700">{{ stats.inactive }}</p>
        <p class="mt-1 text-xs text-slate-600">ซ่อนไว้ ไม่สามารถเลือกสร้างบทความใหม่ได้</p>
      </div>
    </section>

    <!-- STATUS TABS & SEARCH -->
    <div class="rounded-2xl border border-slate-300 bg-white p-4 shadow-xs space-y-4">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <!-- TABS -->
        <div class="flex flex-wrap items-center gap-2 border-b border-slate-300 pb-3 w-full sm:w-auto sm:border-none sm:pb-0">
          <button
            v-for="tab in statusTabs"
            :key="tab.value"
            type="button"
            class="inline-flex items-center gap-2 rounded-xl px-4 py-2 text-xs sm:text-sm font-bold transition"
            :class="
              activeTab === tab.value
                ? 'bg-slate-900 text-white shadow-xs'
                : 'text-slate-700 hover:bg-slate-100 hover:text-slate-900'
            "
            @click="selectTab(tab.value)"
          >
            <i :class="['mdi', tab.icon]" />
            <span>{{ tab.label }}</span>
            <span
              class="rounded-full px-2 py-0.5 text-xs font-bold"
              :class="
                activeTab === tab.value
                  ? 'bg-white/20 text-white'
                  : 'bg-slate-200 text-slate-700'
              "
            >
              {{ getTabCount(tab.value) }}
            </span>
          </button>
        </div>

        <!-- SEARCH INPUT -->
        <div class="relative w-full sm:w-72">
          <i class="mdi mdi-magnify absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 text-base" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="ค้นหาชื่อหมวดหมู่หรือคำอธิบาย..."
            class="w-full rounded-xl border border-slate-300 bg-white py-2 pl-9 pr-8 text-xs sm:text-sm font-bold text-slate-900 placeholder:text-slate-500 focus:border-teal-600 focus:outline-none focus:ring-1 focus:ring-teal-600"
            @input="onSearchInput"
          />
          <button
            v-if="searchQuery"
            type="button"
            class="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-700"
            @click="clearSearch"
          >
            <i class="mdi mdi-close-circle text-sm" />
          </button>
        </div>
      </div>
    </div>

    <!-- MAIN DATA TABLE -->
    <AppDataTable
      :columns="columns"
      :items="categories"
      row-key="contentCategoryId"
      :loading="loading"
      :pagination="pagination"
      :variant="'admin'"
      empty-message="ไม่พบข้อมูลหมวดหมู่คอนเทนต์ตามเงื่อนไขที่เลือก"
      @page-change="changePage"
    >
      <!-- CATEGORY NAME & ID -->
      <template #cell-categoryName="{ item }">
        <div class="flex items-center gap-3 py-1">
          <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-teal-50 text-teal-800 font-bold border border-teal-100">
            <i class="mdi mdi-folder-outline text-xl" />
          </div>
          <div>
            <p class="text-sm font-bold text-slate-900">
              {{ asCategory(item).categoryName }}
            </p>
            <p class="text-xs font-mono text-slate-500 mt-0.5">
              ID: {{ asCategory(item).contentCategoryId.slice(0, 8) }}
            </p>
          </div>
        </div>
      </template>

      <!-- DESCRIPTION -->
      <template #cell-description="{ item }">
        <p class="text-xs sm:text-sm text-slate-700 max-w-md line-clamp-2">
          {{ asCategory(item).description || '-' }}
        </p>
      </template>

      <!-- STATUS BADGE -->
      <template #cell-status="{ item }">
        <span
          class="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-bold"
          :class="
            asCategory(item).status === 'Active'
              ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
              : 'bg-slate-100 text-slate-700 border border-slate-300'
          "
        >
          <i
            class="mdi text-xs"
            :class="asCategory(item).status === 'Active' ? 'mdi-check-circle' : 'mdi-cancel'"
          />
          {{ asCategory(item).status === 'Active' ? 'เปิดใช้งาน' : 'ปิดใช้งาน' }}
        </span>
      </template>

      <!-- ACTIONS COLUMN -->
      <template #cell-actions="{ item }">
        <div class="flex items-center gap-2">
          <!-- EDIT BUTTON -->
          <RouterLink
            :to="`/admin/content-categories/${asCategory(item).contentCategoryId}/edit`"
            class="inline-flex items-center gap-1 rounded-lg border border-teal-200 bg-teal-50 px-3 py-1.5 text-xs font-bold text-teal-800 hover:bg-teal-100 transition"
            title="แก้ไขหมวดหมู่"
          >
            <i class="mdi mdi-pencil-outline text-sm" />
            <span>แก้ไข</span>
          </RouterLink>

          <!-- ACTIVATE BUTTON -->
          <button
            v-if="asCategory(item).status === 'Inactive'"
            type="button"
            class="inline-flex items-center gap-1 rounded-lg border border-emerald-300 bg-emerald-50 px-3 py-1.5 text-xs font-bold text-emerald-700 hover:bg-emerald-100 transition"
            title="เปิดใช้งานหมวดหมู่"
            @click="toggleStatus(asCategory(item), 'Active')"
          >
            <i class="mdi mdi-check-circle-outline text-sm" />
            <span>เปิดใช้งาน</span>
          </button>

          <!-- DEACTIVATE BUTTON -->
          <button
            v-else
            type="button"
            class="inline-flex items-center gap-1 rounded-lg border border-rose-200 bg-rose-50 px-3 py-1.5 text-xs font-bold text-rose-700 hover:bg-rose-100 transition"
            title="ปิดใช้งานหมวดหมู่"
            @click="toggleStatus(asCategory(item), 'Inactive')"
          >
            <i class="mdi mdi-cancel text-sm" />
            <span>ปิดใช้งาน</span>
          </button>
        </div>
      </template>
    </AppDataTable>
  </main>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import {
  getContentCategories,
  setContentCategoryStatus,
  type ContentCategory,
} from '../api/adminContentCategoryApi'
import AppDataTable, {
  type DataTableColumn,
  type DataTablePagination,
} from '@/components/common/AppDataTable.vue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'

const categories = ref<ContentCategory[]>([])
const loading = ref(true)
const activeTab = ref<string>('All')
const searchQuery = ref<string>('')

const pagination = ref<DataTablePagination>({
  page: 1,
  pageSize: 15,
  totalCount: 0,
  totalPages: 0,
})

const stats = reactive({
  all: 0,
  active: 0,
  inactive: 0,
})

const swal = useSwal()

const statusTabs = [
  { value: 'All', label: 'ทั้งหมด', icon: 'mdi-layers-outline' },
  { value: 'Active', label: 'เปิดใช้งาน', icon: 'mdi-check-circle-outline' },
  { value: 'Inactive', label: 'ปิดใช้งาน', icon: 'mdi-cancel' },
]

const columns: DataTableColumn[] = [
  { key: 'categoryName', label: 'หมวดหมู่คอนเทนต์' },
  { key: 'description', label: 'รายละเอียด' },
  { key: 'status', label: 'สถานะ' },
  { key: 'actions', label: 'จัดการ' },
]

function asCategory(item: object): ContentCategory {
  return item as ContentCategory
}

function getTabCount(tabValue: string): number {
  switch (tabValue) {
    case 'Active':
      return stats.active
    case 'Inactive':
      return stats.inactive
    default:
      return stats.all
  }
}

async function loadStats() {
  try {
    const [allRes, activeRes, inactiveRes] = await Promise.allSettled([
      getContentCategories({ page: 1, pageSize: 1 }),
      getContentCategories({ status: 'Active', page: 1, pageSize: 1 }),
      getContentCategories({ status: 'Inactive', page: 1, pageSize: 1 }),
    ])

    if (allRes.status === 'fulfilled') stats.all = allRes.value.totalCount
    if (activeRes.status === 'fulfilled') stats.active = activeRes.value.totalCount
    if (inactiveRes.status === 'fulfilled') stats.inactive = inactiveRes.value.totalCount
  } catch {
    // Non-blocking stats error
  }
}

async function loadCategories() {
  loading.value = true
  try {
    const result = await getContentCategories({
      status: activeTab.value === 'All' ? null : activeTab.value,
      search: searchQuery.value.trim() || null,
      page: pagination.value.page,
      pageSize: pagination.value.pageSize,
    })
    categories.value = result.items
    pagination.value = {
      page: result.page,
      pageSize: result.pageSize,
      totalCount: result.totalCount,
      totalPages: result.totalPages,
    }
  } catch (error) {
    await swal.error(
      'โหลดหมวดหมู่คอนเทนต์ไม่สำเร็จ',
      getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'),
    )
  } finally {
    loading.value = false
  }
}

function selectTab(tab: string) {
  activeTab.value = tab
  pagination.value.page = 1
  loadCategories()
}

function onSearchInput() {
  pagination.value.page = 1
  loadCategories()
}

function clearSearch() {
  searchQuery.value = ''
  pagination.value.page = 1
  loadCategories()
}

function changePage(page: number) {
  pagination.value.page = page
  loadCategories()
}

function refreshData() {
  loadCategories()
  loadStats()
}

async function toggleStatus(category: ContentCategory, targetStatus: 'Active' | 'Inactive') {
  const isActivating = targetStatus === 'Active'
  const actionText = isActivating ? 'เปิดใช้งาน' : 'ปิดใช้งาน'
  const result = await swal.confirm(`ต้องการ${actionText}หมวดหมู่นี้?`, category.categoryName)
  if (!result.isConfirmed) return

  try {
    await setContentCategoryStatus(category.contentCategoryId, category, targetStatus)
    await swal.success(`${actionText}หมวดหมู่เรียบร้อยแล้ว`)
    await loadCategories()
    await loadStats()
  } catch (error) {
    await swal.error('ดำเนินการไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

onMounted(() => {
  loadCategories()
  loadStats()
})
</script>
