<template>
  <main class="mx-auto w-full max-w-[1920px] px-6 py-8 lg:px-10 space-y-6">
    <!-- HEADER -->
    <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 rounded-3xl border border-slate-300 border-l-8 border-l-teal-600 bg-white p-5 shadow-xs sm:p-6">
      <div>
        <div class="inline-flex items-center gap-2 rounded-lg bg-teal-50 px-3 py-1 text-xs font-bold text-teal-800 mb-2">
          <i class="mdi mdi-tag-multiple-outline text-base" />
          <span>TAG MANAGEMENT</span>
        </div>
        <h1 class="text-2xl sm:text-3xl font-bold tracking-tight text-slate-900">
          แท็กคอนเทนต์
        </h1>
        <p class="mt-1 text-sm font-bold text-slate-600">
          จัดการคำสำคัญ (Tags) สำหรับจัดกลุ่ม คัดกรอง และสืบค้นคอนเทนต์ในระบบ
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
          to="/admin/tags/new"
          class="inline-flex items-center gap-2 rounded-xl bg-teal-700 px-5 py-2.5 text-sm font-bold text-white shadow-xs hover:bg-teal-800 transition"
        >
          <i class="mdi mdi-plus text-lg" />
          <span>เพิ่มแท็กใหม่</span>
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
        <p class="mt-1 text-xs text-slate-600">แท็กทั้งหมดในระบบ</p>
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
        <p class="mt-1 text-xs text-slate-600">สามารถติดแท็กในบทความได้</p>
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
        <p class="mt-1 text-xs text-slate-600">ซ่อนไว้ ไม่สามารถเลือกติดแท็กใหม่ได้</p>
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
            placeholder="ค้นหาชื่อแท็ก..."
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
      :items="tags"
      row-key="tagId"
      :loading="loading"
      :pagination="pagination"
      :variant="'admin'"
      empty-message="ไม่พบข้อมูลแท็กตามเงื่อนไขที่เลือก"
      @page-change="changePage"
    >
      <!-- TAG NAME -->
      <template #cell-tagName="{ item }">
        <div class="flex items-center gap-3 py-1">
          <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-teal-50 text-teal-800 font-bold border border-teal-100">
            <i class="mdi mdi-pound text-xl" />
          </div>
          <div>
            <span class="inline-flex items-center rounded-lg bg-teal-50 px-2.5 py-1 text-sm font-bold text-teal-800 border border-teal-200/60">
              #{{ asTag(item).tagName }}
            </span>
            <p class="text-xs font-mono text-slate-500 mt-1">
              ID: {{ asTag(item).tagId.slice(0, 8) }}
            </p>
          </div>
        </div>
      </template>

      <!-- STATUS BADGE -->
      <template #cell-status="{ item }">
        <span
          class="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-bold"
          :class="
            asTag(item).status === 'Active'
              ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
              : 'bg-slate-100 text-slate-700 border border-slate-300'
          "
        >
          <i
            class="mdi text-xs"
            :class="asTag(item).status === 'Active' ? 'mdi-check-circle' : 'mdi-cancel'"
          />
          {{ asTag(item).status === 'Active' ? 'เปิดใช้งาน' : 'ปิดใช้งาน' }}
        </span>
      </template>

      <!-- ACTIONS COLUMN -->
      <template #cell-actions="{ item }">
        <div class="flex items-center gap-2">
          <!-- EDIT BUTTON -->
          <RouterLink
            :to="`/admin/tags/${asTag(item).tagId}/edit`"
            class="inline-flex items-center gap-1 rounded-lg border border-teal-200 bg-teal-50 px-2.5 py-1.5 text-xs font-bold text-teal-800 hover:bg-teal-100 transition"
            title="แก้ไขแท็ก"
          >
            <i class="mdi mdi-pencil-outline text-sm" />
            <span>แก้ไข</span>
          </RouterLink>

          <!-- ACTIVATE BUTTON -->
          <button
            v-if="asTag(item).status === 'Inactive'"
            type="button"
            class="inline-flex items-center gap-1 rounded-lg border border-emerald-300 bg-emerald-50 px-2.5 py-1.5 text-xs font-bold text-emerald-700 hover:bg-emerald-100 transition"
            title="เปิดใช้งานแท็ก"
            @click="toggleStatus(asTag(item), 'Active')"
          >
            <i class="mdi mdi-check-circle-outline text-sm" />
            <span>เปิดใช้งาน</span>
          </button>

          <!-- DEACTIVATE BUTTON -->
          <button
            v-else
            type="button"
            class="inline-flex items-center gap-1 rounded-lg border border-rose-200 bg-rose-50 px-2.5 py-1.5 text-xs font-bold text-rose-700 hover:bg-rose-100 transition"
            title="ปิดใช้งานแท็ก"
            @click="toggleStatus(asTag(item), 'Inactive')"
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
import { getTags, setTagStatus, type Tag } from '../api/adminTagApi'
import AppDataTable, {
  type DataTableColumn,
  type DataTablePagination,
} from '@/components/common/AppDataTable.vue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'

const tags = ref<Tag[]>([])
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
  { key: 'tagName', label: 'ชื่อแท็ก' },
  { key: 'status', label: 'สถานะ' },
  { key: 'actions', label: 'จัดการ' },
]

function asTag(item: object): Tag {
  return item as Tag
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
      getTags({ page: 1, pageSize: 1 }),
      getTags({ status: 'Active', page: 1, pageSize: 1 }),
      getTags({ status: 'Inactive', page: 1, pageSize: 1 }),
    ])

    if (allRes.status === 'fulfilled') stats.all = allRes.value.totalCount
    if (activeRes.status === 'fulfilled') stats.active = activeRes.value.totalCount
    if (inactiveRes.status === 'fulfilled') stats.inactive = inactiveRes.value.totalCount
  } catch {
    // Non-blocking stats error
  }
}

async function loadTags() {
  loading.value = true
  try {
    const result = await getTags({
      status: activeTab.value === 'All' ? null : activeTab.value,
      search: searchQuery.value.trim() || null,
      page: pagination.value.page,
      pageSize: pagination.value.pageSize,
    })
    tags.value = result.items
    pagination.value = {
      page: result.page,
      pageSize: result.pageSize,
      totalCount: result.totalCount,
      totalPages: result.totalPages,
    }
  } catch (error) {
    await swal.error('โหลดข้อมูลแท็กไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}

function selectTab(tab: string) {
  activeTab.value = tab
  pagination.value.page = 1
  loadTags()
}

function onSearchInput() {
  pagination.value.page = 1
  loadTags()
}

function clearSearch() {
  searchQuery.value = ''
  pagination.value.page = 1
  loadTags()
}

function changePage(page: number) {
  pagination.value.page = page
  loadTags()
}

function refreshData() {
  loadTags()
  loadStats()
}

async function toggleStatus(tag: Tag, targetStatus: 'Active' | 'Inactive') {
  const isActivating = targetStatus === 'Active'
  const actionText = isActivating ? 'เปิดใช้งาน' : 'ปิดใช้งาน'
  const result = await swal.confirm(`ต้องการ${actionText}แท็กนี้?`, `#${tag.tagName}`)
  if (!result.isConfirmed) return

  try {
    await setTagStatus(tag.tagId, tag, targetStatus)
    await swal.success(`${actionText}แท็กเรียบร้อยแล้ว`)
    await loadTags()
    await loadStats()
  } catch (error) {
    await swal.error('ดำเนินการไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

onMounted(() => {
  loadTags()
  loadStats()
})
</script>
