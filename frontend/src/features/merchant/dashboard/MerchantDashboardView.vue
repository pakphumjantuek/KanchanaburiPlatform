<script setup lang="ts">
import { formatCurrency, formatDate, formatThaiDateShort } from '@/shared/utils/formatDate'
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import {
  Chart,
  LineController,
  BarController,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js'
import { getMerchantDashboardReport, type MerchantDashboardReport } from '@/features/merchant/api/shopApi'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import { imageUrl } from '@/shared/utils/imageUrl'

Chart.register(
  LineController,
  BarController,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
  Filler,
)

const report = ref<MerchantDashboardReport | null>(null)
const activeMainTab = ref<'trend' | 'promotion'>('trend')
const selectedDealTab = ref<'FlashDeal' | 'CommunityDeal'>('FlashDeal')

const flashDeals = computed(() => {
  return report.value?.promotionAnalytics.deals.filter((d) => d.dealType === 'FlashDeal') ?? []
})

const communityDeals = computed(() => {
  return report.value?.promotionAnalytics.deals.filter((d) => d.dealType !== 'FlashDeal') ?? []
})

const currentTabDeals = computed(() => {
  return selectedDealTab.value === 'FlashDeal' ? flashDeals.value : communityDeals.value
})

const currentTabMetrics = computed(() => {
  const deals = currentTabDeals.value
  const revenue = deals.reduce((sum, d) => sum + (d.revenue || 0), 0)
  const discount = deals.reduce((sum, d) => sum + (d.discountAmount || 0), 0)
  const used = deals.reduce((sum, d) => sum + (d.usedQuantity || 0), 0)
  const total = deals.reduce((sum, d) => sum + (d.totalQuantity || 0), 0)
  const activeCount = deals.filter((d) => d.status === 'Active').length

  return {
    revenue,
    discount,
    used,
    total,
    activeCount,
  }
})

function promotionStatusLabel(status: string) {
  if (status === 'Active') return 'กำลังใช้งาน'
  if (status === 'Stopped') return 'หยุดดีลแล้ว'
  if (status === 'Expired') return 'หมดเวลาแล้ว'
  return status
}

function promotionStatusBadge(status: string) {
  if (status === 'Active') return 'bg-emerald-50 text-emerald-700 border-emerald-200'
  if (status === 'Stopped') return 'bg-slate-100 text-slate-700 border-slate-200'
  return 'bg-amber-50 text-amber-700 border-amber-200'
}

const loading = ref(true)
const chartDays = ref<7 | 30>(7)
const chartType = ref<'line' | 'bar'>('line')
const canvasRef = ref<HTMLCanvasElement | null>(null)
let chartInstance: Chart | null = null
const swal = useSwal()

function getOrderStatusBadge(status: string) {
  switch (status) {
    case 'Pending':
      return 'bg-amber-50 text-amber-700 border-amber-200'
    case 'Paid':
    case 'Processing':
      return 'bg-blue-50 text-blue-700 border-blue-200'
    case 'Shipped':
      return 'bg-indigo-50 text-indigo-700 border-indigo-200'
    case 'Completed':
      return 'bg-emerald-50 text-emerald-700 border-emerald-200'
    case 'Cancelled':
      return 'bg-rose-50 text-rose-700 border-rose-200'
    default:
      return 'bg-slate-100 text-slate-700 border-slate-200'
  }
}

function getOrderStatusText(status: string) {
  switch (status) {
    case 'Pending':
      return 'รอตรวจสอบ'
    case 'Paid':
      return 'ชำระแล้ว'
    case 'Processing':
      return 'กำลังเตรียมส่ง'
    case 'Shipped':
      return 'จัดส่งแล้ว'
    case 'Completed':
      return 'สำเร็จ'
    case 'Cancelled':
      return 'ยกเลิก'
    default:
      return status
  }
}

function renderChart() {
  if (!canvasRef.value || !report.value?.salesTrend) return
  if (chartInstance) {
    chartInstance.destroy()
    chartInstance = null
  }

  const dataSlice = report.value.salesTrend.slice(-chartDays.value)
  const labels = dataSlice.map((x) => formatThaiDateShort(x.date))
  const revenues = dataSlice.map((x) => x.dailyRevenue)
  const orders = dataSlice.map((x) => x.ordersCount)

  const ctx = canvasRef.value.getContext('2d')
  if (!ctx) return

  const lineGradient = ctx.createLinearGradient(0, 0, 0, 300)
  lineGradient.addColorStop(0, 'rgba(5, 150, 105, 0.35)')
  lineGradient.addColorStop(0.6, 'rgba(5, 150, 105, 0.08)')
  lineGradient.addColorStop(1, 'rgba(5, 150, 105, 0.0)')

  const barGradient = ctx.createLinearGradient(0, 0, 0, 300)
  barGradient.addColorStop(0, '#059669')
  barGradient.addColorStop(1, '#10b981')

  chartInstance = new Chart(ctx, {
    type: chartType.value,
    data: {
      labels,
      datasets: [
        {
          label: 'ยอดขาย (บาท)',
          data: revenues,
          borderColor: '#059669',
          backgroundColor: chartType.value === 'line' ? lineGradient : barGradient,
          borderWidth: 2.5,
          fill: true,
          tension: 0.35,
          pointBackgroundColor: '#ffffff',
          pointBorderColor: '#059669',
          pointBorderWidth: 2.5,
          pointRadius: chartDays.value === 7 ? 5 : 3.5,
          pointHoverRadius: 8,
          pointHoverBackgroundColor: '#059669',
          pointHoverBorderColor: '#ffffff',
          pointHoverBorderWidth: 2.5,
          yAxisID: 'y',
          borderRadius: chartType.value === 'bar' ? 6 : 0,
        },
        {
          type: 'line',
          label: 'จำนวนคำสั่งซื้อ',
          data: orders,
          borderColor: '#64748b',
          backgroundColor: 'transparent',
          borderWidth: 2,
          borderDash: [4, 4],
          pointRadius: chartDays.value === 7 ? 4 : 2.5,
          pointBackgroundColor: '#64748b',
          yAxisID: 'y1',
          tension: 0.3,
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: {
        mode: 'index',
        intersect: false,
      },
      plugins: {
        legend: {
          display: true,
          position: 'top',
          align: 'end',
          labels: {
            font: { family: 'sans-serif', size: 12, weight: 'bold' },
            color: '#334155',
            usePointStyle: true,
            boxWidth: 8,
          },
        },
        tooltip: {
          backgroundColor: '#0f172a',
          titleFont: { size: 12, weight: 'bold' },
          bodyFont: { size: 12 },
          padding: 10,
          cornerRadius: 10,
          displayColors: true,
          callbacks: {
            label: function (context) {
              if (context.dataset.label === 'ยอดขาย (บาท)') {
                return ` ยอดขาย: ${new Intl.NumberFormat('th-TH', { style: 'currency', currency: 'THB' }).format(context.parsed.y ?? 0)}`
              }
              return ` คำสั่งซื้อ: ${context.parsed.y} รายการ`
            },
          },
        },
      },
      scales: {
        x: {
          grid: { display: false },
          ticks: { color: '#64748b', font: { size: 11, weight: 'bold' } },
        },
        y: {
          type: 'linear',
          display: true,
          position: 'left',
          grid: { color: 'rgba(226, 232, 240, 0.7)' },
          ticks: {
            color: '#059669',
            font: { size: 11, weight: 'bold' },
            callback: (val) => `฿${val}`,
          },
        },
        y1: {
          type: 'linear',
          display: true,
          position: 'right',
          grid: { display: false },
          ticks: {
            color: '#64748b',
            font: { size: 11, weight: 'bold' },
            stepSize: 1,
            precision: 0,
          },
        },
      },
    },
  })
}

async function load() {
  loading.value = true
  try {
    report.value = await getMerchantDashboardReport()
    loading.value = false
    await nextTick()
    renderChart()
  } catch (error) {
    loading.value = false
    await swal.error('โหลดข้อมูลภาพรวมไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

watch(canvasRef, (newCanvas) => {
  if (newCanvas && report.value) {
    renderChart()
  }
})

watch([chartDays, chartType], async () => {
  await nextTick()
  renderChart()
})

watch(activeMainTab, async (newTab) => {
  if (newTab === 'trend') {
    await nextTick()
    renderChart()
  }
})

onMounted(load)
</script>

<template>
  <div class="space-y-6 text-slate-800 max-w-[1920px] mx-auto pb-10">
    <!-- Header Banner -->
    <div class="flex flex-wrap items-center justify-between gap-4 border-b border-slate-200 pb-5">
      <div>
        <div
          class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-emerald-50 border border-emerald-200 text-xs font-bold text-emerald-800 mb-2"
        >
          <i class="mdi mdi-view-dashboard-outline text-emerald-600"></i>
          <span>ศูนย์ควบคุมสถานประกอบการ (Merchant Portal)</span>
        </div>
        <h1 class="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
          แดชบอร์ดภาพรวมร้านค้า
        </h1>
        <p class="mt-1 text-xs sm:text-sm text-slate-500 font-medium">
          สรุปรายงานยอดขาย คำสั่งซื้อ สต็อกสินค้า และแนวโน้มสถิติการเติบโตของร้านค้า
        </p>
      </div>

      <div class="flex items-center gap-2.5">
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-xl bg-white hover:bg-slate-50 border border-slate-200 px-4 py-2.5 text-xs font-bold text-slate-700 shadow-2xs transition active:scale-95 cursor-pointer"
          :disabled="loading"
          @click="load"
        >
          <i
            class="mdi mdi-refresh text-base text-emerald-600"
            :class="{ 'animate-spin': loading }"
          ></i>
          <span>รีเฟรชข้อมูล</span>
        </button>

        <RouterLink
          to="/my-shop/products/new"
          class="inline-flex items-center gap-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 px-4 py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 cursor-pointer"
        >
          <i class="mdi mdi-plus text-base text-white"></i>
          <span>เพิ่มสินค้าใหม่</span>
        </RouterLink>
      </div>
    </div>

    <!-- Skeleton Loading -->
    <div v-if="loading" class="space-y-6">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <div
          v-for="i in 4"
          :key="i"
          class="h-32 animate-pulse rounded-2xl bg-white border border-slate-200"
        ></div>
      </div>
      <div class="h-96 animate-pulse rounded-2xl bg-white border border-slate-200"></div>
    </div>

    <template v-else-if="report">
      <!-- 1. KPI Summary Cards Grid -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <!-- Revenue Card -->
        <div
          class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs relative overflow-hidden flex flex-col justify-between hover:border-slate-300 transition"
        >
          <div class="flex items-start justify-between">
            <div>
              <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">
                ยอดขายรวมทั้งหมด
              </p>
              <h2 class="mt-2 text-2xl font-black text-slate-900 tracking-tight">
                {{ formatCurrency(report.summary.totalRevenue) }}
              </h2>
            </div>
            <div
              class="flex h-11 w-11 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600 border border-emerald-100 shrink-0"
            >
              <i class="mdi mdi-cash-multiple text-2xl"></i>
            </div>
          </div>
          <div
            class="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-xs font-semibold text-slate-600"
          >
            <span>วันนี้: <strong class="text-slate-900">{{ formatCurrency(report.summary.todayRevenue) }}</strong></span>
            <span>เดือนนี้: <strong class="text-emerald-700">{{ formatCurrency(report.summary.monthRevenue) }}</strong></span>
          </div>
        </div>

        <!-- Total Orders Card -->
        <div
          class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs relative overflow-hidden flex flex-col justify-between hover:border-slate-300 transition"
        >
          <div class="flex items-start justify-between">
            <div>
              <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">
                คำสั่งซื้อทั้งหมด
              </p>
              <h2 class="mt-2 text-2xl font-black text-slate-900 tracking-tight">
                {{ report.summary.totalOrders }}
                <span class="text-xs font-semibold text-slate-500">ออเดอร์</span>
              </h2>
            </div>
            <div
              class="flex h-11 w-11 items-center justify-center rounded-xl bg-blue-50 text-blue-600 border border-blue-100 shrink-0"
            >
              <i class="mdi mdi-clipboard-text-clock-outline text-2xl"></i>
            </div>
          </div>
          <div
            class="mt-4 pt-3 border-t border-slate-100 flex items-center gap-2 text-xs font-bold"
          >
            <span
              class="inline-flex items-center gap-1 text-amber-700 bg-amber-50 px-2 py-0.5 rounded-lg border border-amber-200"
            >
              <i class="mdi mdi-clock-outline"></i> รอตรวจสลิป: {{ report.summary.pendingSlipOrdersCount }}
            </span>
            <span
              class="inline-flex items-center gap-1 text-blue-700 bg-blue-50 px-2 py-0.5 rounded-lg border border-blue-200"
            >
              <i class="mdi mdi-truck-delivery-outline"></i> รอส่ง: {{ report.summary.pendingShipmentOrdersCount }}
            </span>
          </div>
        </div>

        <!-- Products Card -->
        <div
          class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs relative overflow-hidden flex flex-col justify-between hover:border-slate-300 transition"
        >
          <div class="flex items-start justify-between">
            <div>
              <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">สินค้าในร้านค้า</p>
              <h2 class="mt-2 text-2xl font-black text-slate-900 tracking-tight">
                {{ report.summary.totalProducts }}
                <span class="text-xs font-semibold text-slate-500">รายการ</span>
              </h2>
            </div>
            <div
              class="flex h-11 w-11 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600 border border-indigo-100 shrink-0"
            >
              <i class="mdi mdi-package-variant-closed text-2xl"></i>
            </div>
          </div>
          <div
            class="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-xs font-semibold"
          >
            <span
              :class="
                report.summary.lowStockProductsCount > 0
                  ? 'text-rose-600 font-bold inline-flex items-center gap-1'
                  : 'text-slate-500 inline-flex items-center gap-1'
              "
            >
              <i class="mdi mdi-alert-circle-outline"></i> สต็อกต่ำ (≤5): {{ report.summary.lowStockProductsCount }} รายการ
            </span>
            <RouterLink to="/my-shop/products" class="text-emerald-700 hover:underline font-bold">
              ดูสต็อก
            </RouterLink>
          </div>
        </div>

        <!-- Content Stories Card -->
        <div
          class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs relative overflow-hidden flex flex-col justify-between hover:border-slate-300 transition"
        >
          <div class="flex items-start justify-between">
            <div>
              <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">
                เรื่องราว / คอนเทนต์
              </p>
              <h2 class="mt-2 text-2xl font-black text-slate-900 tracking-tight">
                {{ report.summary.totalContents }}
                <span class="text-xs font-semibold text-slate-500">บทความ</span>
              </h2>
            </div>
            <div
              class="flex h-11 w-11 items-center justify-center rounded-xl bg-purple-50 text-purple-600 border border-purple-100 shrink-0"
            >
              <i class="mdi mdi-text-box-multiple-outline text-2xl"></i>
            </div>
          </div>
          <div class="mt-4 pt-3 border-t border-slate-100 text-xs text-slate-500 font-semibold flex items-center justify-between">
            <RouterLink
              to="/my-shop/contents/new"
              class="text-emerald-700 hover:underline font-bold flex items-center gap-1"
            >
              <i class="mdi mdi-plus-circle-outline"></i> เขียนเรื่องราวโปรโมตร้าน
            </RouterLink>
            <RouterLink to="/my-shop/contents" class="text-slate-500 hover:text-slate-800">
              ดูทั้งหมด
            </RouterLink>
          </div>
        </div>
      </div>

      <!-- 2. ANALYTICS SECTION (2 TABS: TREND & PROMOTIONS) -->
      <section class="rounded-2xl border border-slate-200/80 bg-white p-6 shadow-xs space-y-5">
        <!-- Top Nav / Tab Switcher -->
        <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4 border-b border-slate-100 pb-4">
          <!-- 2 Main Tabs -->
          <div class="inline-flex items-center rounded-full bg-slate-100 p-1 border border-slate-200/80 self-start shadow-2xs">
            <!-- Tab 1: Sales & Orders Trend -->
            <button
              type="button"
              @click="activeMainTab = 'trend'"
              class="inline-flex items-center gap-2 px-4 py-2 rounded-full text-xs sm:text-sm font-bold transition-all duration-200 cursor-pointer outline-none focus:outline-none focus:ring-0 focus-visible:outline-none select-none"
              :class="
                activeMainTab === 'trend'
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/50'
              "
            >
              <i class="mdi mdi-chart-line text-base"></i>
              <span>แนวโน้มยอดขายและคำสั่งซื้อ</span>
            </button>

            <!-- Tab 2: Promotion & Deals Analytics -->
            <button
              type="button"
              @click="activeMainTab = 'promotion'"
              class="inline-flex items-center gap-2 px-4 py-2 rounded-full text-xs sm:text-sm font-bold transition-all duration-200 cursor-pointer outline-none focus:outline-none focus:ring-0 focus-visible:outline-none select-none"
              :class="
                activeMainTab === 'promotion'
                  ? 'bg-purple-700 text-white shadow-xs'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/50'
              "
            >
              <i class="mdi mdi-tag-multiple-outline text-base"></i>
              <span>การวิเคราะห์ดีลและโปรโมชัน</span>
              <span
                class="rounded-full px-2 py-0.5 text-[10px] font-black"
                :class="
                  activeMainTab === 'promotion'
                    ? 'bg-purple-900/40 text-white'
                    : 'bg-slate-200 text-slate-700'
                "
              >
                {{ report.promotionAnalytics.deals.length }}
              </span>
            </button>
          </div>

          <!-- Context Controls for Active Tab -->
          <!-- Tab 1 Controls: Chart Type & Time Range -->
          <div v-if="activeMainTab === 'trend'" class="flex flex-wrap items-center gap-2.5">
            <div class="inline-flex rounded-full bg-slate-100 p-1 border border-slate-200/70">
              <button
                type="button"
                class="px-3.5 py-1.5 rounded-full text-xs font-bold transition-all flex items-center gap-1 cursor-pointer outline-none focus:outline-none focus:ring-0 focus-visible:outline-none select-none"
                :class="
                  chartType === 'line'
                    ? 'bg-white text-slate-900 shadow-2xs'
                    : 'text-slate-600 hover:text-slate-900'
                "
                @click="chartType = 'line'"
              >
                <i class="mdi mdi-chart-line"></i> เส้น
              </button>
              <button
                type="button"
                class="px-3.5 py-1.5 rounded-full text-xs font-bold transition-all flex items-center gap-1 cursor-pointer outline-none focus:outline-none focus:ring-0 focus-visible:outline-none select-none"
                :class="
                  chartType === 'bar'
                    ? 'bg-white text-slate-900 shadow-2xs'
                    : 'text-slate-600 hover:text-slate-900'
                "
                @click="chartType = 'bar'"
              >
                <i class="mdi mdi-chart-bar"></i> แท่ง
              </button>
            </div>

            <div class="inline-flex rounded-full bg-slate-100 p-1 border border-slate-200/70">
              <button
                type="button"
                class="px-3.5 py-1.5 rounded-full text-xs font-bold transition-all cursor-pointer outline-none focus:outline-none focus:ring-0 focus-visible:outline-none select-none"
                :class="
                  chartDays === 7
                    ? 'bg-emerald-600 text-white shadow-2xs'
                    : 'text-slate-600 hover:text-slate-900'
                "
                @click="chartDays = 7"
              >
                7 วัน
              </button>
              <button
                type="button"
                class="px-3.5 py-1.5 rounded-full text-xs font-bold transition-all cursor-pointer outline-none focus:outline-none focus:ring-0 focus-visible:outline-none select-none"
                :class="
                  chartDays === 30
                    ? 'bg-emerald-600 text-white shadow-2xs'
                    : 'text-slate-600 hover:text-slate-900'
                "
                @click="chartDays = 30"
              >
                30 วัน
              </button>
            </div>
          </div>

          <!-- Tab 2 Controls: Deal Sub-Filter & Manage Link -->
          <div v-else class="flex flex-wrap items-center gap-2.5">
            <div class="inline-flex rounded-full bg-slate-100 p-1 border border-slate-200/70 text-xs font-bold">
              <button
                type="button"
                @click="selectedDealTab = 'FlashDeal'"
                class="px-3.5 py-1.5 rounded-full transition-all flex items-center gap-1 cursor-pointer outline-none focus:outline-none focus:ring-0 focus-visible:outline-none select-none"
                :class="
                  selectedDealTab === 'FlashDeal'
                    ? 'bg-white text-purple-800 shadow-2xs font-black'
                    : 'text-slate-600 hover:text-slate-900'
                "
              >
                <i class="mdi mdi-flash text-amber-500"></i> Flash Deals ({{ flashDeals.length }})
              </button>
              <button
                type="button"
                @click="selectedDealTab = 'CommunityDeal'"
                class="px-3.5 py-1.5 rounded-full transition-all flex items-center gap-1 cursor-pointer outline-none focus:outline-none focus:ring-0 focus-visible:outline-none select-none"
                :class="
                  selectedDealTab === 'CommunityDeal'
                    ? 'bg-white text-emerald-800 shadow-2xs font-black'
                    : 'text-slate-600 hover:text-slate-900'
                "
              >
                <i class="mdi mdi-account-group-outline text-emerald-600"></i> โปรเด็ดชุมชน ({{ communityDeals.length }})
              </button>
            </div>
            <RouterLink
              to="/my-shop/promotions"
              class="inline-flex items-center gap-1 px-3.5 py-1.5 rounded-full border border-slate-200 bg-white hover:bg-slate-50 text-xs font-bold text-slate-700 transition outline-none focus:outline-none"
            >
              <span>จัดการดีล</span>
              <i class="mdi mdi-arrow-top-right text-xs"></i>
            </RouterLink>
          </div>
        </div>

        <!-- TAB 1: Trend Chart Container -->
        <div v-show="activeMainTab === 'trend'" class="space-y-2">
          <p class="text-xs text-slate-500 font-medium">
            กราฟเปรียบเทียบยอดขายเงินบาทกับจำนวนคำสั่งซื้อในแต่ละวัน (อัปเดตตามคำสั่งซื้อจริง)
          </p>
          <div class="h-80 w-full relative pt-2">
            <canvas ref="canvasRef"></canvas>
          </div>
        </div>

        <!-- TAB 2: Promotion & Deals Analytics Container -->
        <div v-show="activeMainTab === 'promotion'" class="space-y-5">
          <!-- 4 Sub-KPIs -->
          <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div class="rounded-xl bg-slate-50 border border-slate-200/80 p-4">
              <p class="text-xs font-semibold text-slate-500">
                ยอดขาย ({{ selectedDealTab === 'FlashDeal' ? 'Flash Deals' : 'โปรเด็ดชุมชน' }})
              </p>
              <p class="mt-1 text-lg font-black text-slate-900">
                {{ formatCurrency(currentTabMetrics.revenue) }}
              </p>
            </div>
            <div class="rounded-xl bg-slate-50 border border-slate-200/80 p-4">
              <p class="text-xs font-semibold text-slate-500">มูลค่าส่วนลดรวม</p>
              <p class="mt-1 text-lg font-black text-rose-700">
                {{ formatCurrency(currentTabMetrics.discount) }}
              </p>
            </div>
            <div class="rounded-xl bg-slate-50 border border-slate-200/80 p-4">
              <p class="text-xs font-semibold text-slate-500">สิทธิ์ที่ใช้ไปแล้ว</p>
              <p class="mt-1 text-lg font-black text-slate-900">
                {{ currentTabMetrics.used }}
                <span class="text-xs font-semibold text-slate-500">/ {{ currentTabMetrics.total }} สิทธิ์</span>
              </p>
            </div>
            <div class="rounded-xl bg-slate-50 border border-slate-200/80 p-4">
              <p class="text-xs font-semibold text-slate-500">ดีลที่กำลังใช้งาน</p>
              <p class="mt-1 text-lg font-black text-emerald-700">
                {{ currentTabMetrics.activeCount }}
                <span class="text-xs font-semibold text-slate-500">/ {{ currentTabDeals.length }} รายการ</span>
              </p>
            </div>
          </div>

          <!-- Deals Table Filtered by Tab -->
          <div class="overflow-x-auto rounded-xl border border-slate-200/80">
            <table class="w-full text-left text-xs sm:text-sm">
              <thead class="bg-slate-50 text-slate-600 font-bold border-b border-slate-200">
                <tr>
                  <th class="py-3 px-4">ชื่อสินค้า</th>
                  <th class="py-3 px-4">ประเภทดีล</th>
                  <th class="py-3 px-4 min-w-[140px]">สิทธิ์การใช้งาน</th>
                  <th class="py-3 px-4">ยอดขาย</th>
                  <th class="py-3 px-4">ส่วนลดรวม</th>
                  <th class="py-3 px-4 text-center">สถานะ</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr
                  v-if="currentTabDeals.length === 0"
                  class="text-center py-6 text-slate-500"
                >
                  <td colspan="6" class="py-10 text-center text-slate-500 font-medium">
                    <div class="flex flex-col items-center justify-center gap-2">
                      <div class="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                        <i class="mdi mdi-tag-off-outline text-2xl"></i>
                      </div>
                      <p class="text-sm font-semibold text-slate-600">
                        ยังไม่มีรายการดีลในหมวดหมู่นี้
                      </p>
                      <p class="text-xs text-slate-400 max-w-sm">
                        {{ selectedDealTab === 'FlashDeal' ? 'คุณยังไม่ได้เข้าร่วมหรือจัดแคมเปญโปรโมชันพิเศษ (Flash Deals)' : 'คุณยังไม่ได้จัดแคมเปญโปรเด็ดชุมชน (Community Deals)' }}
                      </p>
                    </div>
                  </td>
                </tr>
                <tr
                  v-for="deal in currentTabDeals"
                  :key="deal.productDealId"
                  class="hover:bg-slate-50/60 transition"
                >
                  <td class="py-3.5 px-4 font-bold text-slate-900">
                    <div class="flex items-center gap-2">
                      <span class="truncate max-w-[200px]">{{ deal.productName }}</span>
                    </div>
                  </td>
                  <td class="py-3.5 px-4 font-medium text-slate-600">
                    <span
                      class="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-bold"
                      :class="
                        deal.dealType === 'FlashDeal'
                          ? 'bg-purple-50 text-purple-700 border border-purple-200'
                          : 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                      "
                    >
                      <i
                        class="mdi"
                        :class="deal.dealType === 'FlashDeal' ? 'mdi-flash text-amber-500' : 'mdi-account-group-outline text-emerald-600'"
                      ></i>
                      {{ deal.dealType === 'FlashDeal' ? 'โปรโมชันพิเศษ' : 'โปรเด็ดชุมชน' }}
                    </span>
                  </td>
                  <td class="py-3.5 px-4">
                    <div class="space-y-1">
                      <div class="flex items-center justify-between text-xs font-semibold text-slate-700">
                        <span>{{ deal.usedQuantity }} / {{ deal.totalQuantity }}</span>
                        <span class="text-[11px] text-slate-400">
                          {{ Math.round(((deal.usedQuantity || 0) / (deal.totalQuantity || 1)) * 100) }}%
                        </span>
                      </div>
                      <div class="w-full h-1.5 rounded-full bg-slate-100 overflow-hidden">
                        <div
                          class="h-full rounded-full transition-all duration-500"
                          :class="
                            ((deal.usedQuantity || 0) / (deal.totalQuantity || 1)) >= 0.8
                              ? 'bg-rose-500'
                              : 'bg-emerald-600'
                          "
                          :style="{
                            width: `${Math.min(100, Math.round(((deal.usedQuantity || 0) / (deal.totalQuantity || 1)) * 100))}%`
                          }"
                        ></div>
                      </div>
                    </div>
                  </td>
                  <td class="py-3.5 px-4 font-bold text-slate-900">{{ formatCurrency(deal.revenue) }}</td>
                  <td class="py-3.5 px-4 font-bold text-rose-600">{{ formatCurrency(deal.discountAmount) }}</td>
                  <td class="py-3.5 px-4 text-center">
                    <span
                      class="px-2.5 py-1 rounded-full text-xs font-bold border"
                      :class="promotionStatusBadge(deal.status)"
                    >
                      {{ promotionStatusLabel(deal.status) }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </section>

      <!-- 3. Recent Actionable Orders & Quick Actions Section -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
        <!-- Left: Recent Orders (2 Cols) -->
        <div
          class="lg:col-span-2 rounded-2xl border border-slate-200/80 bg-white p-6 shadow-xs space-y-4"
        >
          <div class="flex items-center justify-between border-b border-slate-100 pb-4">
            <div class="flex items-center gap-2.5">
              <div
                class="flex h-9 w-9 items-center justify-center rounded-xl bg-amber-50 text-amber-600 border border-amber-200 shrink-0"
              >
                <i class="mdi mdi-clock-alert-outline text-lg"></i>
              </div>
              <div>
                <h2 class="text-base font-bold text-slate-900">ออเดอร์ล่าสุดที่ต้องดำเนินการ</h2>
                <p class="text-xs text-slate-500 font-medium">
                  รายการคำสั่งซื้อที่รอการตรวจสอบสลิปหรือรอเตรียมจัดส่ง
                </p>
              </div>
            </div>
            <RouterLink
              to="/my-shop/orders"
              class="text-xs font-bold text-emerald-700 hover:underline flex items-center gap-1"
            >
              ดูทั้งหมด <i class="mdi mdi-chevron-right"></i>
            </RouterLink>
          </div>

          <div
            v-if="report.recentOrders.length === 0"
            class="py-12 text-center text-xs text-slate-500 font-medium space-y-2"
          >
            <i class="mdi mdi-check-circle-outline text-3xl text-emerald-600 block"></i>
            <span>ยังไม่มีคำสั่งซื้อที่ต้องดำเนินการค้างอยู่ในระบบ</span>
          </div>

          <div v-else class="divide-y divide-slate-100">
            <div
              v-for="order in report.recentOrders"
              :key="order.orderId"
              class="py-3.5 flex flex-wrap items-center justify-between gap-3 hover:bg-slate-50/80 px-2.5 rounded-xl transition"
            >
              <div class="space-y-1">
                <div class="flex items-center gap-2 flex-wrap">
                  <span class="font-bold text-sm text-slate-900">{{ order.orderNumber }}</span>
                  <span
                    class="px-2.5 py-0.5 text-xs font-bold rounded-full border"
                    :class="getOrderStatusBadge(order.orderStatus)"
                  >
                    {{ getOrderStatusText(order.orderStatus) }}
                  </span>
                  <span
                    v-if="order.slipImageUrl"
                    class="px-2 py-0.5 text-[11px] font-semibold rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200 inline-flex items-center gap-1"
                  >
                    <i class="mdi mdi-file-check-outline"></i> แนบสลิปแล้ว
                  </span>
                </div>
                <p class="text-xs text-slate-500 font-medium">
                  ผู้ซื้อ: <strong class="text-slate-800">{{ order.receiverName || 'ลูกค้าทั่วไป' }}</strong>
                  <span class="mx-1.5">•</span>
                  {{ order.itemsCount }} รายการ
                  <span class="mx-1.5">•</span>
                  {{ formatDate(order.createdAt, { day: 'numeric', month: 'short', year: '2-digit', hour: '2-digit', minute: '2-digit' }) }}
                </p>
              </div>

              <div class="flex items-center gap-3">
                <span class="text-sm font-black text-slate-900">
                  {{ formatCurrency(order.totalAmount) }}
                </span>
                <RouterLink
                  to="/my-shop/orders"
                  class="px-3 py-1.5 rounded-lg bg-emerald-600 text-white text-xs font-bold hover:bg-emerald-700 transition cursor-pointer shadow-2xs"
                >
                  จัดการออเดอร์
                </RouterLink>
              </div>
            </div>
          </div>
        </div>

        <!-- Right: Quick Actions & Low Stock Alerts -->
        <div class="space-y-6">
          <!-- Quick Menu Shortcuts -->
          <div class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs space-y-3">
            <h2
              class="text-sm font-bold text-slate-900 flex items-center gap-2 border-b border-slate-100 pb-3"
            >
              <i class="mdi mdi-lightning-bolt text-amber-500"></i>
              <span>เมนูลัดจัดการร้านค้า</span>
            </h2>
            <div class="grid grid-cols-2 gap-2.5">
              <RouterLink
                to="/my-shop/products/new"
                class="flex flex-col items-center justify-center p-3 rounded-xl bg-slate-50 hover:bg-emerald-50/60 border border-slate-200 hover:border-emerald-300 transition text-center group cursor-pointer"
              >
                <div class="h-9 w-9 rounded-lg bg-emerald-100 text-emerald-700 flex items-center justify-center group-hover:scale-105 transition-transform">
                  <i class="mdi mdi-plus-box text-xl"></i>
                </div>
                <span class="text-xs font-bold text-slate-800 mt-2">เพิ่มสินค้า</span>
              </RouterLink>

              <RouterLink
                to="/my-shop/contents/new"
                class="flex flex-col items-center justify-center p-3 rounded-xl bg-slate-50 hover:bg-purple-50/60 border border-slate-200 hover:border-purple-300 transition text-center group cursor-pointer"
              >
                <div class="h-9 w-9 rounded-lg bg-purple-100 text-purple-700 flex items-center justify-center group-hover:scale-105 transition-transform">
                  <i class="mdi mdi-square-edit-outline text-xl"></i>
                </div>
                <span class="text-xs font-bold text-slate-800 mt-2">เขียนบทความ</span>
              </RouterLink>

              <RouterLink
                to="/my-shop/orders"
                class="flex flex-col items-center justify-center p-3 rounded-xl bg-slate-50 hover:bg-blue-50/60 border border-slate-200 hover:border-blue-300 transition text-center group cursor-pointer"
              >
                <div class="h-9 w-9 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center group-hover:scale-105 transition-transform">
                  <i class="mdi mdi-format-list-checks text-xl"></i>
                </div>
                <span class="text-xs font-bold text-slate-800 mt-2">รายการคำสั่งซื้อ</span>
              </RouterLink>

              <RouterLink
                to="/my-shop/info"
                class="flex flex-col items-center justify-center p-3 rounded-xl bg-slate-50 hover:bg-slate-100 border border-slate-200 hover:border-slate-300 transition text-center group cursor-pointer"
              >
                <div class="h-9 w-9 rounded-lg bg-slate-200 text-slate-700 flex items-center justify-center group-hover:scale-105 transition-transform">
                  <i class="mdi mdi-store-cog-outline text-xl"></i>
                </div>
                <span class="text-xs font-bold text-slate-800 mt-2">ตั้งค่าร้านค้า</span>
              </RouterLink>
            </div>
          </div>

          <!-- Low Stock Alert Card -->
          <div class="rounded-2xl border border-rose-200 bg-rose-50/30 p-5 shadow-xs space-y-3">
            <div class="flex items-center justify-between border-b border-rose-200/60 pb-3">
              <h2 class="text-sm font-bold text-rose-900 flex items-center gap-2">
                <i class="mdi mdi-alert-circle-outline text-rose-600 text-lg"></i>
                <span>เตือนสินค้าสต็อกต่ำ</span>
              </h2>
              <span class="text-xs font-bold text-rose-700 bg-rose-100 px-2 py-0.5 rounded-md">≤ 5 ชิ้น</span>
            </div>

            <div
              v-if="report.lowStockProducts.length === 0"
              class="py-4 text-center text-xs text-emerald-700 font-bold flex items-center justify-center gap-1.5"
            >
              <i class="mdi mdi-check-circle text-base"></i>
              <span>สินค้าทุกรายการมีสต็อกพร้อมขาย</span>
            </div>

            <div v-else class="space-y-2.5">
              <div
                v-for="prod in report.lowStockProducts"
                :key="prod.productId"
                class="flex items-center justify-between gap-3 p-2.5 rounded-xl bg-white border border-rose-200 shadow-2xs"
              >
                <div class="flex items-center gap-2.5 min-w-0">
                  <img
                    v-if="prod.imageUrl"
                    :src="imageUrl(prod.imageUrl)"
                    :alt="prod.productName"
                    class="h-9 w-9 rounded-lg object-cover shrink-0 border border-slate-100"
                  />
                  <div class="min-w-0">
                    <p class="text-xs font-bold text-slate-900 truncate">{{ prod.productName }}</p>
                    <p class="text-[11px] font-semibold text-slate-500">
                      {{ formatCurrency(prod.price) }}
                    </p>
                  </div>
                </div>
                <span
                  class="px-2.5 py-1 rounded-lg bg-rose-600 text-white font-bold text-xs shrink-0"
                >
                  เหลือ {{ prod.quantity }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 4. TOP PRODUCTS & REVIEWS & FULFILLMENT -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
        <!-- Top Selling Products (1 Col) -->
        <div class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs space-y-4">
          <div class="flex items-center justify-between border-b border-slate-100 pb-3">
            <h2 class="text-sm font-bold text-slate-900 flex items-center gap-2">
              <i class="mdi mdi-trophy-outline text-lg text-amber-500"></i>
              <span>สินค้าขายดี 5 อันดับแรก</span>
            </h2>
            <RouterLink
              to="/my-shop/products"
              class="text-xs font-bold text-emerald-700 hover:underline"
            >
              ทั้งหมด
            </RouterLink>
          </div>

          <div
            v-if="report.topProducts.length === 0"
            class="py-8 text-center text-xs text-slate-500 font-medium"
          >
            ยังไม่มีข้อมูลสินค้าขายดีในขณะนี้
          </div>

          <div v-else class="space-y-2.5">
            <div
              v-for="(prod, index) in report.topProducts"
              :key="prod.productId"
              class="flex items-center justify-between gap-3 p-2.5 rounded-xl bg-slate-50/70 border border-slate-100"
            >
              <div class="flex items-center gap-2.5 min-w-0">
                <span
                  class="flex h-6 w-6 items-center justify-center rounded-lg font-black text-xs shrink-0"
                  :class="
                    index === 0
                      ? 'bg-amber-400 text-amber-950 shadow-2xs'
                      : index === 1
                        ? 'bg-slate-300 text-slate-800'
                        : index === 2
                          ? 'bg-amber-700 text-white'
                          : 'bg-slate-200 text-slate-600'
                  "
                >
                  {{ index + 1 }}
                </span>
                <img
                  v-if="prod.imageUrl"
                  :src="imageUrl(prod.imageUrl)"
                  :alt="prod.productName"
                  class="h-9 w-9 rounded-lg object-cover shrink-0 border border-slate-200"
                />
                <div class="min-w-0">
                  <p class="text-xs font-bold text-slate-900 truncate">{{ prod.productName }}</p>
                  <p class="text-[11px] font-semibold text-slate-500">
                    {{ formatCurrency(prod.price) }}
                  </p>
                </div>
              </div>

              <div class="text-right shrink-0">
                <p class="text-xs font-bold text-slate-900">
                  ขาย {{ prod.totalUnitsSold }} ชิ้น
                </p>
                <p class="text-[11px] font-bold text-emerald-700">
                  {{ formatCurrency(prod.totalRevenue) }}
                </p>
              </div>
            </div>
          </div>
        </div>

        <!-- Shop Reviews & Rating (1 Col) -->
        <div class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs space-y-4">
          <div class="flex items-center justify-between border-b border-slate-100 pb-3">
            <h2 class="text-sm font-bold text-slate-900 flex items-center gap-2">
              <i class="mdi mdi-star text-lg text-amber-500"></i>
              <span>คะแนนและรีวิวล่าสุด</span>
            </h2>
            <span
              class="text-xs font-bold text-amber-800 bg-amber-50 px-2.5 py-0.5 rounded-lg border border-amber-200"
            >
              {{ report.reviewsSummary.averageRating }} / 5.0 ({{ report.reviewsSummary.totalReviews }} รีวิว)
            </span>
          </div>

          <div
            v-if="report.reviewsSummary.recentReviews.length === 0"
            class="py-8 text-center text-xs text-slate-500 font-medium"
          >
            ยังไม่มีรีวิวสำหรับร้านค้าของคุณ
          </div>

          <div v-else class="space-y-2.5">
            <div
              v-for="rev in report.reviewsSummary.recentReviews"
              :key="rev.reviewId"
              class="p-3 rounded-xl bg-slate-50/70 border border-slate-100 space-y-1.5"
            >
              <div class="flex items-center justify-between text-xs">
                <span class="font-bold text-slate-900">{{ rev.userName }}</span>
                <span class="text-amber-500 font-bold text-sm tracking-widest">
                  {{ '★'.repeat(rev.rating) }}{{ '☆'.repeat(5 - rev.rating) }}
                </span>
              </div>
              <p class="text-xs text-slate-600 font-medium leading-relaxed">"{{ rev.comment }}"</p>
              <p class="text-[10px] text-slate-400 text-right">
                {{ formatDate(rev.createdAt, { day: 'numeric', month: 'short', year: '2-digit', hour: '2-digit', minute: '2-digit' }) }}
              </p>
            </div>
          </div>
        </div>

        <!-- Fulfillment Breakdown & Tips (1 Col) -->
        <div class="space-y-5">
          <!-- Fulfillment & AOV Card -->
          <div class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs space-y-3">
            <h2
              class="text-sm font-bold text-slate-900 flex items-center gap-2 border-b border-slate-100 pb-3"
            >
              <i class="mdi mdi-truck-check-outline text-lg text-emerald-600"></i>
              <span>สัดส่วนจัดส่งและยอดเฉลี่ย</span>
            </h2>

            <div
              class="rounded-xl border border-slate-200/80 bg-slate-50 px-3.5 py-2.5 text-xs font-bold text-slate-800 flex items-center justify-between"
            >
              <span>ออเดอร์จัดส่งพัสดุ</span>
              <span class="text-emerald-700 font-black">{{ report.fulfillment.deliveryOrdersCount }} รายการ</span>
            </div>

            <div class="pt-2 border-t border-slate-100 flex items-center justify-between text-xs">
              <span class="text-slate-500 font-medium">ยอดขายเฉลี่ยต่อออเดอร์ (AOV):</span>
              <strong class="font-black text-slate-900 text-sm">
                {{ formatCurrency(report.fulfillment.averageOrderValue) }}
              </strong>
            </div>
          </div>

          <!-- Merchant Growth Tips Card -->
          <div
            class="rounded-2xl border border-emerald-200 bg-emerald-50/40 p-5 text-xs text-slate-800 space-y-2.5"
          >
            <h3 class="font-bold text-emerald-950 text-sm flex items-center gap-1.5">
              <i class="mdi mdi-lightbulb-on text-emerald-600 text-base"></i>
              <span>คำแนะนำเพิ่มยอดขายร้านค้า</span>
            </h3>
            <ul class="space-y-1.5 text-slate-600 font-medium pt-1">
              <li class="flex items-start gap-1.5">
                <i class="mdi mdi-check-circle text-emerald-600 text-sm shrink-0 mt-0.5"></i>
                <span>เขียนคอนเทนต์หรือเรื่องราวร้านค้าเพื่อดึงดูดลูกค้าและสร้างความน่าเชื่อถือ</span>
              </li>
              <li class="flex items-start gap-1.5">
                <i class="mdi mdi-check-circle text-emerald-600 text-sm shrink-0 mt-0.5"></i>
                <span>ตรวจสอบสลิปและอัปเดตสถานะจัดส่งให้รวดเร็วเพื่อสร้างความประทับใจ</span>
              </li>
              <li class="flex items-start gap-1.5">
                <i class="mdi mdi-check-circle text-emerald-600 text-sm shrink-0 mt-0.5"></i>
                <span>คอยตรวจสอบสต็อกสินค้าและเติมสินค้าที่ใกล้หมดอยู่เสมอ</span>
              </li>
            </ul>
          </div>
        </div>
      </div>

    </template>
  </div>
</template>

<style scoped>
.scrollbar-thin::-webkit-scrollbar {
  width: 4px;
}
.scrollbar-thin::-webkit-scrollbar-track {
  background: #f1f5f9;
}
.scrollbar-thin::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 9999px;
}

button {
  outline: none !important;
  -webkit-tap-highlight-color: transparent;
}
button:focus,
button:focus-visible {
  outline: none !important;
}
</style>

