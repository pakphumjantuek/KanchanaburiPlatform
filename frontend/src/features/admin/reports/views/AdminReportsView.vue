<script setup lang="ts">
import { formatDate } from '@/shared/utils/formatDate'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import {
  getReports,
  updateReportStatus,
  type Report,
  type ReportStatus,
} from '../api/adminReportApi'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'

const reports = ref<Report[]>([])
const activeTab = ref<'all' | ReportStatus>('all')
const targetTypeFilter = ref<'all' | 'content' | 'review'>('all')
const searchQuery = ref('')
const sortBy = ref<'newest' | 'oldest'>('newest')

const loading = ref(true)
const updatingId = ref<string | null>(null)
const selectedReport = ref<Report | null>(null)
const copiedId = ref<string | null>(null)
const swal = useSwal()

const statusOptions: { value: ReportStatus; label: string; icon: string; badgeClass: string }[] = [
  {
    value: 'Pending',
    label: 'รอตรวจสอบ',
    icon: 'mdi-clock-outline',
    badgeClass: 'bg-amber-50 text-amber-800 border-amber-200',
  },
  {
    value: 'InProgress',
    label: 'กำลังดำเนินการ',
    icon: 'mdi-progress-wrench',
    badgeClass: 'bg-sky-50 text-sky-800 border-sky-200',
  },
  {
    value: 'Resolved',
    label: 'ดำเนินการแล้ว',
    icon: 'mdi-check-circle-outline',
    badgeClass: 'bg-emerald-50 text-emerald-800 border-emerald-200',
  },
  {
    value: 'Dismissed',
    label: 'ไม่พบปัญหา',
    icon: 'mdi-close-circle-outline',
    badgeClass: 'bg-slate-100 text-slate-700 border-slate-300',
  },
]

function getStatusMeta(status: ReportStatus) {
  return (
    statusOptions.find((item) => item.value === status) ?? {
      value: status,
      label: status,
      icon: 'mdi-help-circle-outline',
      badgeClass: 'bg-slate-100 text-slate-700 border-slate-300',
    }
  )
}

async function loadData() {
  loading.value = true
  try {
    reports.value = await getReports()
  } catch (error) {
    await swal.error('โหลดรายการรายงานไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}

// Counts for KPI & Tabs
const pendingCount = computed(() => reports.value.filter((r) => r.status === 'Pending').length)
const inProgressCount = computed(
  () => reports.value.filter((r) => r.status === 'InProgress').length,
)
const resolvedCount = computed(() => reports.value.filter((r) => r.status === 'Resolved').length)
const dismissedCount = computed(() => reports.value.filter((r) => r.status === 'Dismissed').length)

// Filtered and Sorted Reports
const filteredReports = computed(() => {
  let list = reports.value

  // Status Filter
  if (activeTab.value !== 'all') {
    list = list.filter((r) => r.status === activeTab.value)
  }

  // Type Filter
  if (targetTypeFilter.value === 'content') {
    list = list.filter((r) => !!r.contentId)
  } else if (targetTypeFilter.value === 'review') {
    list = list.filter((r) => !!r.reviewId)
  }

  // Search Filter
  const q = searchQuery.value.trim().toLowerCase()
  if (q) {
    list = list.filter((r) => {
      const matchLabel = r.targetLabel.toLowerCase().includes(q)
      const matchReason = r.reason.toLowerCase().includes(q)
      const matchDesc = (r.description || '').toLowerCase().includes(q)
      const matchId = r.reportId.toLowerCase().includes(q)
      return matchLabel || matchReason || matchDesc || matchId
    })
  }

  // Sort
  return [...list].sort((a, b) => {
    const timeA = new Date(a.createdAt).getTime()
    const timeB = new Date(b.createdAt).getTime()
    return sortBy.value === 'newest' ? timeB - timeA : timeA - timeB
  })
})

async function applyStatusChange(report: Report, nextStatus: ReportStatus) {
  if (report.status === nextStatus) return

  const meta = getStatusMeta(nextStatus)
  const confirm = await swal.confirm(
    `ปรับสถานะเป็น "${meta.label}"?`,
    `รายงาน: ${report.targetLabel}`,
  )
  if (!confirm.isConfirmed) return

  updatingId.value = report.reportId
  try {
    const updated = await updateReportStatus(report.reportId, nextStatus)
    Object.assign(report, updated)
    if (selectedReport.value && selectedReport.value.reportId === report.reportId) {
      Object.assign(selectedReport.value, updated)
    }
    await swal.success(`อัปเดตสถานะเป็น "${meta.label}" เรียบร้อยแล้ว`)
  } catch (error) {
    await swal.error('อัปเดตสถานะไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    updatingId.value = null
  }
}

async function copyToClipboard(text: string, id: string) {
  try {
    await navigator.clipboard.writeText(text)
    copiedId.value = id
    setTimeout(() => {
      if (copiedId.value === id) copiedId.value = null
    }, 2000)
  } catch {
    // clipboard fallback
  }
}

function openDetailModal(report: Report) {
  selectedReport.value = report
}

function closeDetailModal() {
  selectedReport.value = null
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && selectedReport.value) {
    closeDetailModal()
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
  loadData()
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <main class="mx-auto w-full max-w-[1920px] px-6 py-8 lg:px-10 space-y-7 text-slate-800">
    <!-- Header Banner -->
    <div class="flex flex-wrap items-center justify-between gap-4 border-b border-slate-300/80 pb-6">
      <div>
        <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-rose-50 border border-rose-200 text-xs font-bold text-rose-700 mb-2">
          <i class="mdi mdi-shield-alert-outline text-rose-600"></i>
          <span>ระบบตรวจสอบและระงับเนื้อหา</span>
        </div>
        <h1 class="text-2xl sm:text-3xl font-black tracking-tight text-slate-900">
          ศูนย์จัดการรายงานปัญหาและการละเมิด
        </h1>
        <p class="mt-1 text-xs sm:text-sm text-slate-600 font-bold">
          ตรวจสอบและพิจารณาข้อร้องเรียนเกี่ยวกับคอนเทนต์และรีวิวที่ไม่เหมาะสมจากผู้ใช้งานในระบบ
        </p>
      </div>

      <div class="flex items-center gap-3">
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-xl bg-white hover:bg-slate-50 border border-slate-300 px-4 py-2.5 text-xs font-bold text-slate-700 shadow-xs transition active:scale-95 cursor-pointer hover:border-slate-300"
          :disabled="loading"
          @click="loadData"
        >
          <i class="mdi mdi-refresh text-base text-rose-600" :class="{ 'animate-spin': loading }"></i>
          <span>รีเฟรชข้อมูล</span>
        </button>
      </div>
    </div>

    <!-- KPI Metric Cards Grid -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <!-- Total Reports -->
      <div
        class="rounded-2xl border border-slate-300 bg-white p-5 shadow-xs flex items-center justify-between cursor-pointer hover:border-slate-300 transition"
        :class="{ 'ring-2 ring-slate-800/10 border-slate-400': activeTab === 'all' }"
        @click="activeTab = 'all'"
      >
        <div>
          <span class="text-xs font-bold text-slate-600 uppercase tracking-wider block">รายงานทั้งหมด</span>
          <div class="mt-1.5 flex items-baseline gap-2">
            <span class="text-2xl sm:text-3xl font-black text-slate-900">{{ reports.length }}</span>
            <span class="text-xs font-bold text-slate-600">รายการ</span>
          </div>
        </div>
        <div class="h-12 w-12 rounded-2xl bg-slate-100 text-slate-700 flex items-center justify-center text-2xl border border-slate-300">
          <i class="mdi mdi-clipboard-text-outline"></i>
        </div>
      </div>

      <!-- Pending -->
      <div
        class="rounded-2xl border border-slate-300 bg-white p-5 shadow-xs flex items-center justify-between cursor-pointer hover:border-amber-300 transition"
        :class="{ 'ring-2 ring-amber-500/20 border-amber-300': activeTab === 'Pending' }"
        @click="activeTab = 'Pending'"
      >
        <div>
          <span class="text-xs font-bold text-slate-600 uppercase tracking-wider block">รอตรวจสอบ</span>
          <div class="mt-1.5 flex items-baseline gap-2">
            <span class="text-2xl sm:text-3xl font-black text-amber-700">{{ pendingCount }}</span>
            <span class="text-xs font-bold text-amber-700 bg-amber-50 border border-amber-200 px-2 py-0.5 rounded-full">
              เร่งด่วน
            </span>
          </div>
        </div>
        <div class="h-12 w-12 rounded-2xl bg-amber-50 text-amber-700 flex items-center justify-center text-2xl border border-amber-200">
          <i class="mdi mdi-clock-alert-outline"></i>
        </div>
      </div>

      <!-- In Progress -->
      <div
        class="rounded-2xl border border-slate-300 bg-white p-5 shadow-xs flex items-center justify-between cursor-pointer hover:border-sky-300 transition"
        :class="{ 'ring-2 ring-sky-500/20 border-sky-300': activeTab === 'InProgress' }"
        @click="activeTab = 'InProgress'"
      >
        <div>
          <span class="text-xs font-bold text-slate-600 uppercase tracking-wider block">กำลังดำเนินการ</span>
          <div class="mt-1.5 flex items-baseline gap-2">
            <span class="text-2xl sm:text-3xl font-black text-sky-700">{{ inProgressCount }}</span>
            <span class="text-xs font-bold text-slate-600">รายการ</span>
          </div>
        </div>
        <div class="h-12 w-12 rounded-2xl bg-sky-50 text-sky-700 flex items-center justify-center text-2xl border border-sky-200">
          <i class="mdi mdi-progress-wrench"></i>
        </div>
      </div>

      <!-- Resolved -->
      <div
        class="rounded-2xl border border-slate-300 bg-white p-5 shadow-xs flex items-center justify-between cursor-pointer hover:border-emerald-300 transition"
        :class="{ 'ring-2 ring-emerald-500/20 border-emerald-300': activeTab === 'Resolved' }"
        @click="activeTab = 'Resolved'"
      >
        <div>
          <span class="text-xs font-bold text-slate-600 uppercase tracking-wider block">ดำเนินการแล้ว</span>
          <div class="mt-1.5 flex items-baseline gap-2">
            <span class="text-2xl sm:text-3xl font-black text-emerald-700">{{ resolvedCount }}</span>
            <span class="text-xs font-bold text-slate-600">รายการ</span>
          </div>
        </div>
        <div class="h-12 w-12 rounded-2xl bg-emerald-50 text-emerald-700 flex items-center justify-center text-2xl border border-emerald-200">
          <i class="mdi mdi-check-circle-outline"></i>
        </div>
      </div>
    </div>

    <!-- Status Tabs Navigation -->
    <div class="flex items-center gap-2 border-b border-slate-300 overflow-x-auto pb-px">
      <button
        type="button"
        class="px-4 py-3 text-xs font-bold border-b-2 transition flex items-center gap-2 cursor-pointer whitespace-nowrap"
        :class="activeTab === 'all' ? 'border-slate-900 text-slate-900' : 'border-transparent text-slate-600 hover:text-slate-800 hover:border-slate-300'"
        @click="activeTab = 'all'"
      >
        <span>ทั้งหมด</span>
        <span class="px-2 py-0.5 rounded-full text-[10px] bg-slate-100 text-slate-700 font-bold">
          {{ reports.length }}
        </span>
      </button>

      <button
        v-for="opt in statusOptions"
        :key="opt.value"
        type="button"
        class="px-4 py-3 text-xs font-bold border-b-2 transition flex items-center gap-2 cursor-pointer whitespace-nowrap"
        :class="activeTab === opt.value ? 'border-slate-900 text-slate-900' : 'border-transparent text-slate-600 hover:text-slate-800 hover:border-slate-300'"
        @click="activeTab = opt.value"
      >
        <i class="mdi text-sm" :class="opt.icon"></i>
        <span>{{ opt.label }}</span>
        <span
          class="px-2 py-0.5 rounded-full text-[10px] font-bold"
          :class="opt.value === 'Pending' && pendingCount > 0 ? 'bg-amber-100 text-amber-800' : 'bg-slate-100 text-slate-700'"
        >
          {{
            opt.value === 'Pending'
              ? pendingCount
              : opt.value === 'InProgress'
                ? inProgressCount
                : opt.value === 'Resolved'
                  ? resolvedCount
                  : dismissedCount
          }}
        </span>
      </button>
    </div>

    <!-- Filter & Search Controls Bar -->
    <div class="rounded-2xl border border-slate-300 bg-white p-4 shadow-xs flex flex-wrap items-center justify-between gap-4">
      <div class="flex flex-1 items-center gap-3 min-w-[280px]">
        <div class="relative w-full max-w-md">
          <i class="mdi mdi-magnify absolute left-3.5 top-1/2 -translate-y-1/2 text-lg text-slate-500"></i>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="ค้นหาชื่อคอนเทนต์, เหตุผล, รายละเอียด หรือรหัสรายงาน..."
            class="w-full rounded-xl border border-slate-300 bg-slate-50/60 py-2.5 pl-10 pr-4 text-xs font-bold text-slate-800 placeholder-slate-400 focus:bg-white focus:outline-none focus:ring-2 focus:ring-rose-500/20 focus:border-rose-500 transition"
          />
          <button
            v-if="searchQuery"
            type="button"
            class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-700 cursor-pointer"
            @click="searchQuery = ''"
          >
            <i class="mdi mdi-close-circle text-base"></i>
          </button>
        </div>

        <div class="text-xs font-bold text-slate-600 shrink-0 hidden sm:block">
          แสดง {{ filteredReports.length }} จาก {{ reports.length }} รายการ
        </div>
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <!-- Target Type Filter -->
        <div class="flex items-center gap-1.5">
          <label class="text-xs font-bold text-slate-600 shrink-0">ประเภท:</label>
          <select
            v-model="targetTypeFilter"
            class="rounded-xl border border-slate-300 bg-white px-3 py-2 text-xs font-bold text-slate-700 outline-none focus:border-rose-500 cursor-pointer shadow-2xs"
          >
            <option value="all">ทั้งหมด</option>
            <option value="content">เฉพาะคอนเทนต์</option>
            <option value="review">เฉพาะรีวิว</option>
          </select>
        </div>

        <!-- Sort Filter -->
        <div class="flex items-center gap-1.5">
          <label class="text-xs font-bold text-slate-600 shrink-0">เรียงตาม:</label>
          <select
            v-model="sortBy"
            class="rounded-xl border border-slate-300 bg-white px-3 py-2 text-xs font-bold text-slate-700 outline-none focus:border-rose-500 cursor-pointer shadow-2xs"
          >
            <option value="newest">ล่าสุดก่อน (Newest)</option>
            <option value="oldest">เก่าสุดก่อน (Oldest)</option>
          </select>
        </div>
      </div>
    </div>

    <!-- Skeleton Loading -->
    <div v-if="loading" class="space-y-4">
      <div v-for="i in 3" :key="i" class="h-44 animate-pulse rounded-2xl bg-white border border-slate-300 shadow-xs"></div>
    </div>

    <!-- Empty State: No reports -->
    <div
      v-else-if="reports.length === 0"
      class="rounded-3xl border border-slate-300 bg-white p-14 text-center space-y-4 shadow-xs"
    >
      <div class="h-20 w-20 rounded-3xl bg-emerald-50 text-emerald-600 flex items-center justify-center text-4xl mx-auto shadow-xs border border-emerald-100">
        <i class="mdi mdi-shield-check-outline"></i>
      </div>
      <div class="max-w-md mx-auto space-y-1.5">
        <h3 class="text-xl font-black text-slate-900">ไม่มีรายงานปัญหาในระบบ</h3>
        <p class="text-xs text-slate-600 font-bold">
          ยังไม่มีการรายงานเนื้อหาหรือข้อร้องเรียนจากผู้ใช้งานในระบบ
        </p>
      </div>
    </div>

    <!-- Empty State: Filter produced no matches -->
    <div
      v-else-if="filteredReports.length === 0"
      class="rounded-3xl border border-slate-300 bg-white p-12 text-center space-y-3 shadow-xs"
    >
      <div class="h-14 w-14 rounded-2xl bg-slate-100 text-slate-500 flex items-center justify-center text-2xl mx-auto">
        <i class="mdi mdi-file-search-outline"></i>
      </div>
      <h3 class="text-base font-bold text-slate-900">ไม่พบรายงานที่ตรงกับเงื่อนไขการค้นหา</h3>
      <p class="text-xs text-slate-600 font-bold">ลองเปลี่ยนคำค้นหา หรือรีเซ็ตตัวกรอง</p>
      <button
        type="button"
        class="inline-flex items-center gap-1.5 text-xs font-bold text-rose-600 hover:underline cursor-pointer pt-1"
        @click="searchQuery = ''; activeTab = 'all'; targetTypeFilter = 'all'"
      >
        <i class="mdi mdi-backspace-outline"></i> ล้างตัวกรองทั้งหมด
      </button>
    </div>

    <!-- Report Cards List -->
    <div v-else class="space-y-4">
      <article
        v-for="report in filteredReports"
        :key="report.reportId"
        class="rounded-2xl border border-slate-300 bg-white shadow-xs hover:border-slate-300 transition overflow-hidden"
      >
        <!-- Card Top Bar -->
        <div class="flex flex-wrap items-center justify-between gap-3 p-4 sm:px-6 bg-slate-50/60 border-b border-slate-200 text-xs">
          <!-- Left: Target type & Report ID -->
          <div class="flex flex-wrap items-center gap-2.5">
            <span
              v-if="report.contentId"
              class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-teal-50 border border-teal-200 text-teal-800 font-bold text-[11px]"
            >
              <i class="mdi mdi-newspaper-variant-outline"></i> คอนเทนต์
            </span>
            <span
              v-else-if="report.reviewId"
              class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-amber-50 border border-amber-200 text-amber-800 font-bold text-[11px]"
            >
              <i class="mdi mdi-comment-text-outline"></i> รีวิว
            </span>
            <span
              v-else
              class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-slate-100 border border-slate-300 text-slate-700 font-bold text-[11px]"
            >
              <i class="mdi mdi-flag-outline"></i> รายงานทั่วไป
            </span>

            <button
              type="button"
              class="inline-flex items-center gap-1 text-[11px] font-mono font-bold text-slate-700 hover:text-teal-700 bg-white border border-slate-300 px-2 py-0.5 rounded-lg cursor-pointer transition active:scale-95"
              title="คัดลอกรหัสรายงาน"
              @click="copyToClipboard(report.reportId, `rep-${report.reportId}`)"
            >
              <span>#{{ report.reportId.slice(0, 8) }}</span>
              <i
                class="mdi text-[10px]"
                :class="copiedId === `rep-${report.reportId}` ? 'mdi-check text-emerald-600' : 'mdi-content-copy text-slate-500'"
              ></i>
            </button>

            <span class="text-slate-500">•</span>
            <span class="text-slate-600 font-bold flex items-center gap-1">
              <i class="mdi mdi-clock-outline text-slate-500"></i>
              {{ formatDate(report.createdAt, { dateStyle: 'medium', timeStyle: 'short' }) }}
            </span>
          </div>

          <!-- Right: Status Badge -->
          <div class="flex items-center gap-2">
            <span
              class="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-bold border"
              :class="getStatusMeta(report.status).badgeClass"
            >
              <i class="mdi text-sm" :class="getStatusMeta(report.status).icon"></i>
              <span>{{ getStatusMeta(report.status).label }}</span>
            </span>
          </div>
        </div>

        <!-- Card Body -->
        <div class="p-5 sm:p-6 grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
          <div class="lg:col-span-8 space-y-3">
            <div>
              <span class="text-[11px] font-bold uppercase tracking-wider text-slate-500 block mb-1">
                เป้าหมายที่ถูกรายงาน:
              </span>
              <h2 class="text-base font-bold text-slate-900 leading-snug">
                {{ report.targetLabel }}
              </h2>
            </div>

            <!-- Reason Pill -->
            <div class="flex items-center gap-2">
              <span class="text-xs font-bold text-slate-600">เหตุผลที่รายงาน:</span>
              <span class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg bg-rose-50 border border-rose-200 text-rose-800 text-xs font-bold">
                <i class="mdi mdi-alert-circle-outline"></i>
                {{ report.reason }}
              </span>
            </div>

            <!-- Description quote box if available -->
            <div
              v-if="report.description"
              class="rounded-xl border border-slate-300 bg-slate-50/70 p-3.5 text-xs leading-relaxed text-slate-700 space-y-1"
            >
              <span class="text-[10px] font-bold text-slate-500 uppercase tracking-wider block">
                รายละเอียดเพิ่มเติมจากผู้แจ้ง:
              </span>
              <p class="whitespace-pre-line font-bold text-slate-800">
                {{ report.description }}
              </p>
            </div>
          </div>

          <!-- Quick Actions & Status Workflow (4 cols) -->
          <div class="lg:col-span-4 rounded-xl border border-slate-300 bg-slate-50/50 p-4 space-y-3">
            <div class="flex items-center justify-between">
              <span class="text-xs font-bold text-slate-700">การดำเนินการ</span>
              <button
                type="button"
                class="text-xs font-bold text-teal-700 hover:text-teal-900 cursor-pointer flex items-center gap-1"
                @click="openDetailModal(report)"
              >
                <span>ดูรายละเอียด</span>
                <i class="mdi mdi-arrow-right text-xs"></i>
              </button>
            </div>

            <!-- Status Quick Select Menu -->
            <div class="space-y-1.5">
              <label class="block text-[11px] font-bold text-slate-600">เปลี่ยนสถานะการดำเนินการ:</label>
              <select
                :value="report.status"
                class="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 text-xs font-bold text-slate-800 outline-none focus:border-rose-500 cursor-pointer shadow-2xs disabled:opacity-50"
                :disabled="updatingId === report.reportId"
                @change="applyStatusChange(report, ($event.target as HTMLSelectElement).value as ReportStatus)"
              >
                <option v-for="item in statusOptions" :key="item.value" :value="item.value">
                  {{ item.label }}
                </option>
              </select>
            </div>

            <!-- Target Link Button (if contentId exists) -->
            <div v-if="report.contentId" class="pt-1 border-t border-slate-300">
              <RouterLink
                :to="`/contents/${report.contentId}`"
                target="_blank"
                class="w-full rounded-xl bg-white hover:bg-slate-100 border border-slate-300 px-3 py-2 text-xs font-bold text-slate-700 flex items-center justify-center gap-1.5 shadow-2xs transition"
              >
                <i class="mdi mdi-open-in-new text-teal-700"></i>
                <span>ตรวจสอบคอนเทนต์ต้นฉบับ</span>
              </RouterLink>
            </div>
          </div>
        </div>
      </article>
    </div>

    <!-- Detailed Inspection Modal -->
    <div
      v-if="selectedReport"
      class="fixed inset-0 z-50 bg-black/70 flex items-center justify-center p-4 backdrop-blur-xs"
      @click="closeDetailModal"
    >
      <div
        class="relative w-full max-w-2xl bg-white rounded-3xl shadow-2xl overflow-hidden border border-slate-300 space-y-6"
        @click.stop
      >
        <!-- Modal Top Bar -->
        <div class="flex items-center justify-between px-6 py-4 border-b border-slate-200 bg-slate-50/80">
          <div class="flex items-center gap-3">
            <div class="h-10 w-10 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center font-bold text-xl border border-rose-200">
              <i class="mdi mdi-shield-alert-outline"></i>
            </div>
            <div>
              <h3 class="text-base font-bold text-slate-900">รายละเอียดรายงานข้อร้องเรียน</h3>
              <p class="text-xs text-slate-600 font-mono">รหัสรายงาน: {{ selectedReport.reportId }}</p>
            </div>
          </div>

          <button
            type="button"
            class="h-8 w-8 rounded-lg bg-white hover:bg-slate-100 text-slate-500 hover:text-slate-700 flex items-center justify-center text-lg cursor-pointer"
            @click="closeDetailModal"
          >
            <i class="mdi mdi-close"></i>
          </button>
        </div>

        <!-- Modal Content Body -->
        <div class="px-6 space-y-5 text-xs">
          <!-- Status & Date -->
          <div class="flex flex-wrap items-center justify-between gap-3 p-3.5 rounded-xl bg-slate-50 border border-slate-300">
            <div>
              <span class="text-slate-600 block mb-0.5">สถานะปัจจุบัน</span>
              <span
                class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full font-bold border"
                :class="getStatusMeta(selectedReport.status).badgeClass"
              >
                <i class="mdi text-xs" :class="getStatusMeta(selectedReport.status).icon"></i>
                {{ getStatusMeta(selectedReport.status).label }}
              </span>
            </div>

            <div class="text-right">
              <span class="text-slate-600 block mb-0.5">วันเวลาที่ได้รับรายงาน</span>
              <strong class="text-slate-800">
                {{ formatDate(selectedReport.createdAt, { dateStyle: 'long', timeStyle: 'short' }) }}
              </strong>
            </div>
          </div>

          <!-- Target Label -->
          <div class="space-y-1">
            <span class="font-bold text-slate-600 uppercase tracking-wider block">เป้าหมายที่ถูกรายงาน:</span>
            <div class="p-3 rounded-xl bg-slate-50 border border-slate-300 font-bold text-slate-900 text-sm">
              {{ selectedReport.targetLabel }}
            </div>
          </div>

          <!-- Reason -->
          <div class="space-y-1">
            <span class="font-bold text-slate-600 uppercase tracking-wider block">เหตุผล:</span>
            <div class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 font-bold">
              <i class="mdi mdi-alert-circle-outline"></i>
              <span>{{ selectedReport.reason }}</span>
            </div>
          </div>

          <!-- Description -->
          <div class="space-y-1">
            <span class="font-bold text-slate-600 uppercase tracking-wider block">คำอธิบายเพิ่มเติมจากผู้แจ้ง:</span>
            <div class="p-3.5 rounded-xl bg-slate-50 border border-slate-300 whitespace-pre-line text-slate-800 leading-relaxed font-bold min-h-[70px]">
              {{ selectedReport.description || 'ไม่มีคำอธิบายเพิ่มเติมระบุไว้' }}
            </div>
          </div>
        </div>

        <!-- Modal Bottom Actions -->
        <div class="flex flex-wrap items-center justify-between gap-3 px-6 py-4 border-t border-slate-200 bg-slate-50/50">
          <div class="flex items-center gap-2">
            <RouterLink
              v-if="selectedReport.contentId"
              :to="`/contents/${selectedReport.contentId}`"
              target="_blank"
              class="px-4 py-2 rounded-xl bg-white hover:bg-slate-100 border border-slate-300 text-xs font-bold text-slate-700 flex items-center gap-1.5 shadow-2xs transition"
            >
              <i class="mdi mdi-open-in-new text-teal-700"></i>
              <span>เปิดดูคอนเทนต์</span>
            </RouterLink>
          </div>

          <div class="flex items-center gap-2">
            <button
              v-if="selectedReport.status !== 'Resolved'"
              type="button"
              class="px-4 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold shadow-xs transition cursor-pointer flex items-center gap-1.5"
              :disabled="updatingId === selectedReport.reportId"
              @click="applyStatusChange(selectedReport, 'Resolved')"
            >
              <i class="mdi mdi-check-circle-outline"></i>
              <span>แก้ไข/ดำเนินการแล้ว</span>
            </button>

            <button
              v-if="selectedReport.status !== 'Dismissed'"
              type="button"
              class="px-4 py-2 rounded-xl bg-slate-200 hover:bg-slate-300 text-slate-800 text-xs font-bold transition cursor-pointer flex items-center gap-1.5"
              :disabled="updatingId === selectedReport.reportId"
              @click="applyStatusChange(selectedReport, 'Dismissed')"
            >
              <i class="mdi mdi-close-circle-outline"></i>
              <span>ไม่พบปัญหา</span>
            </button>

            <button
              type="button"
              class="px-4 py-2 rounded-xl border border-slate-300 bg-white hover:bg-slate-100 text-slate-700 text-xs font-bold cursor-pointer"
              @click="closeDetailModal"
            >
              ปิดหน้าต่าง
            </button>
          </div>
        </div>
      </div>
    </div>
  </main>
</template>
