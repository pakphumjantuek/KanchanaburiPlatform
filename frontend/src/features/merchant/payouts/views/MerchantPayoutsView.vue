<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import http from '@/shared/api/http'
import { useSwal } from '@/plugins/sweetalert'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { getMyShop, type Shop } from '@/features/merchant/api/shopApi'
import { formatCurrency, formatDate } from '@/shared/utils/formatDate'
import { imageUrl } from '@/shared/utils/imageUrl'
import type { MerchantPayoutRecord, MerchantPayoutSummary } from '../interface/type'

const summary = ref<MerchantPayoutSummary | null>(null)
const shop = ref<Shop | null>(null)
const loading = ref(true)
const selectedSlip = ref<{ url: string; ref?: string; amount?: number; date?: string } | null>(null)
const expandedPayoutIds = ref<Set<string>>(new Set())
const searchQuery = ref('')
const filterType = ref<'all' | 'withSlip' | 'withNote'>('all')
const swal = useSwal()

async function loadData() {
  loading.value = true
  try {
    const [payoutsRes, shopRes] = await Promise.allSettled([
      http.get<MerchantPayoutSummary>('/shops/mine/payouts'),
      getMyShop(),
    ])

    if (payoutsRes.status === 'fulfilled') {
      summary.value = payoutsRes.value.data
    } else {
      await swal.error('โหลดข้อมูลสรุปการรับเงินไม่สำเร็จ', getApiErrorMessage(payoutsRes.reason, 'กรุณาลองใหม่อีกครั้ง'))
    }

    if (shopRes.status === 'fulfilled') {
      shop.value = shopRes.value
    }
  } catch (error) {
    await swal.error('เกิดข้อผิดพลาด', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}

function toggleExpand(payoutId: string) {
  if (expandedPayoutIds.value.has(payoutId)) {
    expandedPayoutIds.value.delete(payoutId)
  } else {
    expandedPayoutIds.value.add(payoutId)
  }
}

// Total order count across all payout history
const totalOrdersPaidCount = computed(() => {
  if (!summary.value?.history) return 0
  return summary.value.history.reduce((acc, r) => acc + (r.ordersCount || r.orders?.length || 0), 0)
})

// Filtered and searched payout history
const filteredHistory = computed(() => {
  if (!summary.value?.history) return []
  let list = [...summary.value.history]

  // Filter tab
  if (filterType.value === 'withSlip') {
    list = list.filter((r) => Boolean(r.slipImageUrl))
  } else if (filterType.value === 'withNote') {
    list = list.filter((r) => Boolean(r.note && r.note.trim().length > 0))
  }

  // Search query
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim()
    list = list.filter((r) => {
      const matchRef = r.transactionRef?.toLowerCase().includes(q)
      const matchNote = r.note?.toLowerCase().includes(q)
      const matchAmount = r.totalAmount.toString().includes(q)
      const matchOrder = r.orders?.some((o) => o.orderNumber?.toLowerCase().includes(q))
      return matchRef || matchNote || matchAmount || matchOrder
    })
  }

  return list
})

function viewSlip(record: MerchantPayoutRecord) {
  if (!record.slipImageUrl) return
  selectedSlip.value = {
    url: imageUrl(record.slipImageUrl),
    ref: record.transactionRef,
    amount: record.totalAmount,
    date: record.createdAt,
  }
}

onMounted(loadData)
</script>

<template>
  <div class="space-y-6 text-slate-900 max-w-[1920px] mx-auto pb-16">
    <!-- Header Banner -->
    <div class="flex flex-wrap items-center justify-between gap-4 border-b-2 border-slate-200 pb-6">
      <div>
        <div class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-emerald-50 border border-emerald-200 text-xs font-bold text-emerald-800 mb-2">
          <i class="mdi mdi-cash-multiple text-emerald-600"></i>
          <span>ระบบประวัติการรับเงินโอนยอดขาย</span>
        </div>
        <h1 class="text-2xl sm:text-3xl font-black text-slate-950 tracking-tight">
          ประวัติการรับเงินยอดขาย (Payouts)
        </h1>
        <p class="mt-1 text-xs sm:text-sm text-slate-600 font-bold">
          ตรวจสอบยอดเงินขายสินค้า สถานะการโอนเงินคืนจากแอดมิน และใบสลิปหลักฐาน
        </p>
      </div>

      <!-- Action Button (Capsule Style) -->
      <button
        type="button"
        class="inline-flex items-center gap-2 rounded-full bg-white hover:bg-slate-50 border-2 border-slate-200 hover:border-slate-300 px-5 py-2.5 text-xs font-bold text-slate-800 shadow-xs transition active:scale-95 cursor-pointer select-none outline-none focus:outline-none"
        :disabled="loading"
        @click="loadData"
      >
        <i class="mdi mdi-refresh text-base text-emerald-700" :class="{ 'animate-spin': loading }"></i>
        <span>รีเฟรชข้อมูล</span>
      </button>
    </div>

    <!-- Skeleton Loading -->
    <div v-if="loading" class="space-y-6">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div v-for="i in 4" :key="i" class="h-32 animate-pulse rounded-2xl bg-white border-2 border-slate-200"></div>
      </div>
      <div class="h-96 animate-pulse rounded-2xl bg-white border-2 border-slate-200"></div>
    </div>

    <template v-else-if="summary">
      <!-- 1. KPI Summary Cards Grid (4 Columns) -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Card 1: Pending Payout -->
        <div class="rounded-2xl border-2 border-amber-200 bg-white p-5 shadow-xs flex flex-col justify-between space-y-3">
          <div class="flex items-start justify-between">
            <div>
              <span class="text-xs font-bold text-amber-800 uppercase tracking-wide">
                ยอดเงินรอแอดมินโอน
              </span>
              <h2 class="mt-1 text-2xl sm:text-3xl font-black text-amber-600 tracking-tight">
                {{ formatCurrency(summary.pendingPayoutAmount) }}
              </h2>
            </div>
            <div class="flex h-11 w-11 items-center justify-center rounded-2xl bg-amber-100 text-amber-800 shrink-0">
              <i class="mdi mdi-clock-outline text-2xl"></i>
            </div>
          </div>
          <p class="text-xs text-slate-600 font-bold border-t border-slate-100 pt-2.5">
            ยอดจากออเดอร์ที่ชำระเงินแล้ว อยู่ระหว่างรอบสรุปยอดเพื่อโอนเข้าบัญชี
          </p>
        </div>

        <!-- Card 2: Total Paid Out -->
        <div class="rounded-2xl border-2 border-emerald-200 bg-white p-5 shadow-xs flex flex-col justify-between space-y-3">
          <div class="flex items-start justify-between">
            <div>
              <span class="text-xs font-bold text-emerald-800 uppercase tracking-wide">
                ยอดเงินที่ได้รับโอนแล้วทั้งหมด
              </span>
              <h2 class="mt-1 text-2xl sm:text-3xl font-black text-emerald-700 tracking-tight">
                {{ formatCurrency(summary.totalPaidOutAmount) }}
              </h2>
            </div>
            <div class="flex h-11 w-11 items-center justify-center rounded-2xl bg-emerald-100 text-emerald-800 shrink-0">
              <i class="mdi mdi-check-decagram text-2xl"></i>
            </div>
          </div>
          <p class="text-xs text-slate-600 font-bold border-t border-slate-100 pt-2.5">
            ยอดเงินขายทั้งหมดที่แอดมินโอนเข้าบัญชีธนาคารเรียบร้อยแล้ว
          </p>
        </div>

        <!-- Card 3: Payout Transfers Count -->
        <div class="rounded-2xl border-2 border-slate-200 bg-white p-5 shadow-xs flex flex-col justify-between space-y-3">
          <div class="flex items-start justify-between">
            <div>
              <span class="text-xs font-bold text-slate-600 uppercase tracking-wide">
                รอบการโอนเงินสะสม
              </span>
              <h2 class="mt-1 text-2xl sm:text-3xl font-black text-slate-950 tracking-tight">
                {{ summary.history.length }} <span class="text-sm font-bold text-slate-500">รอบ</span>
              </h2>
            </div>
            <div class="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-100 text-slate-700 shrink-0">
              <i class="mdi mdi-receipt-text-outline text-2xl"></i>
            </div>
          </div>
          <p class="text-xs text-slate-600 font-bold border-t border-slate-100 pt-2.5">
            ครอบคลุมคำสั่งซื้อทั้งหมด {{ totalOrdersPaidCount }} คำสั่งซื้อ
          </p>
        </div>

        <!-- Card 4: Receiving Bank Account Info -->
        <div class="rounded-2xl border-2 border-slate-200 bg-white p-5 shadow-xs flex flex-col justify-between space-y-3">
          <div class="flex items-start justify-between">
            <div>
              <span class="text-xs font-bold text-indigo-700 uppercase tracking-wide">
                บัญชีรับเงินร้านค้า
              </span>
              <p class="mt-1 text-sm font-black text-slate-900 truncate">
                {{ shop?.bankName || 'ยังไม่ระบุธนาคาร' }}
              </p>
              <p class="text-xs font-bold text-slate-700 font-mono">
                {{ shop?.bankAccountNumber || (shop?.promptPay ? `พร้อมเพย์: ${shop.promptPay}` : 'กรุณาตั้งค่าบัญชี') }}
              </p>
            </div>
            <div class="flex h-11 w-11 items-center justify-center rounded-2xl bg-indigo-50 text-indigo-700 shrink-0 border border-indigo-100">
              <i class="mdi mdi-bank-outline text-2xl"></i>
            </div>
          </div>
          <p class="text-[11px] text-slate-500 font-bold border-t border-slate-100 pt-2.5 truncate">
            ชื่อบัญชี: {{ shop?.bankAccountName || shop?.shopName || 'ร้านค้า' }}
          </p>
        </div>
      </div>

      <!-- 2. Payout History Section -->
      <section class="rounded-2xl border-2 border-slate-200 bg-white p-5 sm:p-6 shadow-xs space-y-5">
        <!-- Section Header + Controls (Capsule segmented pills) -->
        <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4 border-b-2 border-slate-100 pb-5">
          <div class="flex items-center gap-3">
            <span class="flex h-10 w-10 items-center justify-center rounded-2xl bg-emerald-100 text-emerald-800 font-bold">
              <i class="mdi mdi-history text-2xl text-emerald-800"></i>
            </span>
            <div>
              <h2 class="text-lg font-black text-slate-950">ประวัติการโอนเงินจากแอดมิน</h2>
              <p class="text-xs font-bold text-slate-600">
                แสดง {{ filteredHistory.length }} จาก {{ summary.history.length }} รายการ
              </p>
            </div>
          </div>

          <!-- Filter Pills & Search (All Capsule Design) -->
          <div class="flex flex-wrap items-center gap-3">
            <!-- Filter Pills (Segmented Capsule) -->
            <div class="inline-flex items-center rounded-full bg-slate-100 p-1 border-2 border-slate-300">
              <button
                type="button"
                class="rounded-full px-3.5 py-1.5 text-xs font-bold transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none"
                :class="
                  filterType === 'all'
                    ? 'bg-emerald-600 text-white shadow-sm'
                    : 'text-slate-700 hover:text-slate-950 bg-transparent'
                "
                @click="filterType = 'all'"
              >
                ทั้งหมด ({{ summary.history.length }})
              </button>
              <button
                type="button"
                class="rounded-full px-3.5 py-1.5 text-xs font-bold transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none"
                :class="
                  filterType === 'withSlip'
                    ? 'bg-emerald-600 text-white shadow-sm'
                    : 'text-slate-700 hover:text-slate-950 bg-transparent'
                "
                @click="filterType = 'withSlip'"
              >
                มีสลิปหลักฐาน
              </button>
              <button
                type="button"
                class="rounded-full px-3.5 py-1.5 text-xs font-bold transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none"
                :class="
                  filterType === 'withNote'
                    ? 'bg-emerald-600 text-white shadow-sm'
                    : 'text-slate-700 hover:text-slate-950 bg-transparent'
                "
                @click="filterType = 'withNote'"
              >
                มีหมายเหตุ
              </button>
            </div>

            <!-- Search Input (Capsule Style) -->
            <div class="relative min-w-[200px] sm:min-w-[240px]">
              <i class="mdi mdi-magnify absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400 text-base"></i>
              <input
                v-model="searchQuery"
                type="text"
                placeholder="ค้นหา Ref, ยอดเงิน, เลขออเดอร์..."
                class="w-full rounded-full border-2 border-slate-200 bg-white py-1.5 pl-9 pr-8 text-xs font-bold text-slate-900 placeholder:text-slate-400 focus:border-emerald-600 focus:bg-white focus:outline-none transition shadow-2xs"
              />
              <button
                v-if="searchQuery"
                type="button"
                class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-700 cursor-pointer text-xs"
                @click="searchQuery = ''"
              >
                <i class="mdi mdi-close-circle"></i>
              </button>
            </div>
          </div>
        </div>

        <!-- Empty State -->
        <div
          v-if="filteredHistory.length === 0"
          class="py-16 text-center text-xs text-slate-500 font-bold space-y-3"
        >
          <div class="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-slate-100 text-slate-400">
            <i class="mdi mdi-text-box-search-outline text-3xl"></i>
          </div>
          <p class="font-black text-base text-slate-900">
            {{ searchQuery || filterType !== 'all' ? 'ไม่พบรายการที่ตรงกับเงื่อนไข' : 'ยังไม่มีประวัติการรับเงินโอนจากแอดมิน' }}
          </p>
          <p class="max-w-md mx-auto text-slate-600">
            {{ searchQuery || filterType !== 'all' ? 'ลองล้างคำค้นหาหรือเปลี่ยนแท็บตัวกรองเพื่อดูรายการทั้งหมด' : 'เมื่อแอดมินสั่งโอนเงินยอดขายให้แก่ร้านค้า รายการและสลิปหลักฐานจะแสดงขึ้นที่นี่' }}
          </p>
          <button
            v-if="searchQuery || filterType !== 'all'"
            type="button"
            class="rounded-full px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition cursor-pointer"
            @click="searchQuery = ''; filterType = 'all'"
          >
            ล้างตัวกรองทั้งหมด
          </button>
        </div>

        <!-- History Records Cards List -->
        <div v-else class="space-y-4">
          <div
            v-for="record in filteredHistory"
            :key="record.payoutId"
            class="rounded-2xl border-2 border-slate-200 bg-white p-5 space-y-4 shadow-xs hover:border-emerald-400 transition-all duration-200"
          >
            <!-- Top Summary Row -->
            <div class="flex flex-wrap items-center justify-between gap-4 border-b border-slate-100 pb-4">
              <div class="space-y-1">
                <div class="flex items-center gap-2.5 flex-wrap">
                  <span class="font-black text-base text-slate-950">โอนเงินยอดขายสำเร็จ</span>
                  <span class="inline-flex items-center gap-1.5 px-3 py-0.5 rounded-full bg-emerald-100 text-emerald-900 text-xs font-bold border border-emerald-300">
                    <span class="h-2 w-2 rounded-full bg-emerald-600"></span>
                    <span>ได้รับเงินแล้ว</span>
                  </span>
                  <span v-if="record.transactionRef" class="inline-flex items-center gap-1 font-mono text-xs font-bold text-slate-700 bg-slate-100 px-2.5 py-0.5 rounded-full border border-slate-200">
                    <i class="mdi mdi-pound text-slate-400 text-xs"></i>
                    <span>Ref: {{ record.transactionRef }}</span>
                  </span>
                </div>

                <div class="flex items-center gap-2 text-xs font-bold text-slate-600 flex-wrap">
                  <span class="flex items-center gap-1 text-slate-700">
                    <i class="mdi mdi-calendar-clock text-emerald-700 text-sm"></i>
                    <span>{{ formatDate(record.createdAt, { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }) }}</span>
                  </span>
                  <span>•</span>
                  <span>ครอบคลุม {{ record.ordersCount || record.orders?.length || 0 }} คำสั่งซื้อ</span>
                </div>
              </div>

              <!-- Amount & Slip Action Button (Capsule Style) -->
              <div class="flex items-center gap-3.5 self-start sm:self-auto flex-wrap">
                <div class="text-right">
                  <span class="block text-[11px] font-bold text-slate-500 uppercase">ยอดเงินที่ได้รับโอน</span>
                  <span class="text-2xl sm:text-3xl font-black text-emerald-700 tracking-tight">
                    {{ formatCurrency(record.totalAmount) }}
                  </span>
                </div>

                <!-- View Slip Button (Capsule) -->
                <button
                  v-if="record.slipImageUrl"
                  type="button"
                  class="inline-flex items-center gap-1.5 rounded-full px-4 py-2 bg-emerald-50 hover:bg-emerald-600 hover:text-white border-2 border-emerald-600 text-xs font-black text-emerald-800 transition active:scale-95 cursor-pointer shadow-2xs select-none outline-none focus:outline-none"
                  @click="viewSlip(record)"
                >
                  <i class="mdi mdi-eye-outline text-base"></i>
                  <span>ดูสลิปโอนเงิน</span>
                </button>
              </div>
            </div>

            <!-- Admin Note (Zero Emojis, Clean Pill-like card) -->
            <div
              v-if="record.note"
              class="flex items-start gap-2.5 text-xs text-amber-950 font-bold bg-amber-50/80 p-3 rounded-xl border border-amber-200"
            >
              <i class="mdi mdi-message-text-outline text-amber-700 text-base shrink-0 mt-0.5"></i>
              <div class="leading-relaxed">
                <span class="font-black text-amber-900">หมายเหตุจากผู้ดูแลระบบ:</span>
                <span class="ml-1 text-slate-900 font-semibold">{{ record.note }}</span>
              </div>
            </div>

            <!-- Expandable Orders Covered Accordion Button (Capsule) -->
            <div v-if="record.orders && record.orders.length > 0" class="pt-1">
              <button
                type="button"
                class="inline-flex items-center gap-1.5 rounded-full px-3.5 py-1.5 text-xs font-bold border border-slate-200 bg-slate-50 hover:bg-slate-100 text-slate-700 hover:text-slate-950 transition cursor-pointer select-none"
                @click="toggleExpand(record.payoutId)"
              >
                <i
                  class="mdi text-sm text-emerald-700 transition-transform duration-200"
                  :class="expandedPayoutIds.has(record.payoutId) ? 'mdi-chevron-up' : 'mdi-chevron-down'"
                ></i>
                <span>
                  {{ expandedPayoutIds.has(record.payoutId) ? 'ซ่อนรายการคำสั่งซื้อ' : `ดูคำสั่งซื้อในรอบนี้ (${record.orders.length} ออเดอร์)` }}
                </span>
              </button>

              <!-- Orders List Drawer -->
              <div
                v-if="expandedPayoutIds.has(record.payoutId)"
                class="mt-3 rounded-2xl border border-slate-200 bg-slate-50 p-3.5 space-y-2.5 transition"
              >
                <p class="text-xs font-black text-slate-800">
                  รายการออเดอร์ที่ถูกสรุปยอดในการโอนรอบนี้:
                </p>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-2.5">
                  <div
                    v-for="order in record.orders"
                    :key="order.orderId"
                    class="rounded-xl border border-slate-200 bg-white p-3 space-y-1.5 shadow-2xs"
                  >
                    <div class="flex items-center justify-between text-xs font-black">
                      <span class="text-slate-900 font-mono">#{{ order.orderNumber }}</span>
                      <span class="text-emerald-700">{{ formatCurrency(order.totalAmount) }}</span>
                    </div>
                    <div class="text-[11px] text-slate-500 font-bold">
                      วันที่: {{ formatDate(order.createdAt, { day: 'numeric', month: 'short', year: 'numeric' }) }}
                    </div>
                    <div v-if="order.items && order.items.length > 0" class="border-t border-slate-100 pt-1.5 space-y-1">
                      <div
                        v-for="item in order.items"
                        :key="item.orderItemId"
                        class="flex items-center justify-between text-[11px] text-slate-700 font-medium"
                      >
                        <span class="truncate max-w-[200px]">{{ item.productName }}</span>
                        <span>x{{ item.quantity }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
    </template>

    <!-- Modal for Zoomed Admin Payout Slip Image (Modern Glassmorphism & Capsule Design) -->
    <div
      v-if="selectedSlip"
      class="fixed inset-0 z-50 bg-slate-950/80 flex items-center justify-center p-4 backdrop-blur-md animate-fade-in"
      @click="selectedSlip = null"
    >
      <div
        class="relative w-full max-w-xl max-h-[92vh] bg-white p-6 rounded-3xl shadow-2xl space-y-4 flex flex-col"
        @click.stop
      >
        <!-- Modal Header -->
        <div class="flex items-center justify-between border-b-2 border-slate-100 pb-3">
          <div class="flex items-center gap-2.5">
            <span class="flex h-9 w-9 items-center justify-center rounded-full bg-emerald-100 text-emerald-800">
              <i class="mdi mdi-receipt text-lg text-emerald-800"></i>
            </span>
            <div>
              <h3 class="text-base font-black text-slate-950">หลักฐานสลิปการโอนเงินจากแอดมิน</h3>
              <p v-if="selectedSlip.ref" class="text-xs font-mono font-bold text-slate-600">
                Ref: {{ selectedSlip.ref }}
              </p>
            </div>
          </div>

          <button
            type="button"
            class="flex h-8 w-8 items-center justify-center rounded-full bg-slate-100 hover:bg-slate-200 text-slate-600 hover:text-slate-900 transition cursor-pointer select-none"
            @click="selectedSlip = null"
          >
            <i class="mdi mdi-close text-lg"></i>
          </button>
        </div>

        <!-- Slip Image Box -->
        <div class="flex-1 overflow-auto flex items-center justify-center bg-slate-900 rounded-2xl p-2 min-h-[300px]">
          <img
            :src="selectedSlip.url"
            alt="หลักฐานสลิปโอนเงิน"
            class="max-h-[62vh] w-auto max-w-full object-contain rounded-xl shadow-lg"
          />
        </div>

        <!-- Modal Footer Controls (Capsule Buttons) -->
        <div class="flex items-center justify-between gap-3 border-t-2 border-slate-100 pt-3">
          <div v-if="selectedSlip.amount" class="text-xs font-bold text-slate-700">
            ยอดเงินที่ระบุ:
            <span class="text-sm font-black text-emerald-700 ml-1">
              {{ formatCurrency(selectedSlip.amount) }}
            </span>
          </div>

          <div class="flex items-center gap-2 ml-auto">
            <a
              :href="selectedSlip.url"
              target="_blank"
              class="inline-flex items-center gap-1.5 rounded-full px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition select-none cursor-pointer"
            >
              <i class="mdi mdi-open-in-new text-xs text-slate-600"></i>
              <span>เปิดดูภาพเต็ม</span>
            </a>
            <button
              type="button"
              class="rounded-full px-5 py-2 bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold transition select-none cursor-pointer"
              @click="selectedSlip = null"
            >
              ปิดหน้าต่าง
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
