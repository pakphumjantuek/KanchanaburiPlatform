<script setup lang="ts">
import { formatCurrency, formatDate } from '@/shared/utils/formatDate'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import http from '@/shared/api/http'
import { useSwal } from '@/plugins/sweetalert'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import type { AdminMerchantPayoutGroup, MerchantPayoutRecord } from '../interface/types'
import { imageUrl } from '@/shared/utils/imageUrl'

const activeTab = ref<'pending' | 'history'>('pending')
const payouts = ref<AdminMerchantPayoutGroup[]>([])
const history = ref<MerchantPayoutRecord[]>([])
const loading = ref(true)
const historyLoading = ref(false)
const copiedId = ref<string | null>(null)
const swal = useSwal()

// Search & Sort State
const searchQuery = ref('')
const sortBy = ref<'amount_desc' | 'amount_asc' | 'orders_desc' | 'newest'>('amount_desc')

// Payout Modal State
const selectedShopForPayout = ref<AdminMerchantPayoutGroup | null>(null)
const payoutFile = ref<File | null>(null)
const payoutPreviewUrl = ref<string>('')
const transactionRef = ref('')
const note = ref('')
const submittingPayout = ref(false)

// Lightbox Modal for Slips
const selectedSlipUrl = ref<string | null>(null)
const zoomScale = ref(1)
const rotation = ref(0)

async function loadData() {
  if (activeTab.value === 'history') {
    await Promise.all([loadPendingPayouts(), loadPayoutHistory()])
  } else {
    await loadPendingPayouts()
  }
}

async function loadPendingPayouts() {
  loading.value = true
  try {
    const { data } = await http.get<AdminMerchantPayoutGroup[]>('/orders/admin/payouts')
    payouts.value = data
  } catch (error) {
    await swal.error('โหลดข้อมูลการโอนเงินไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}

async function loadPayoutHistory() {
  historyLoading.value = true
  try {
    const { data } = await http.get<MerchantPayoutRecord[]>('/orders/admin/payouts/history')
    history.value = data
  } catch (error) {
    await swal.error('โหลดประวัติการโอนเงินไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    historyLoading.value = false
  }
}

function selectTab(tab: 'pending' | 'history') {
  activeTab.value = tab
  if (tab === 'history' && history.value.length === 0) {
    loadPayoutHistory()
  }
}

// KPI Computations
const totalPendingAmount = computed(() => {
  return payouts.value.reduce((acc, p) => acc + (p.totalSalesAmount || 0), 0)
})

const totalPendingOrders = computed(() => {
  return payouts.value.reduce((acc, p) => acc + (p.paidOrdersCount || 0), 0)
})

const totalSettledAmount = computed(() => {
  return history.value.reduce((acc, h) => acc + (h.totalAmount || 0), 0)
})

// Filtered and Sorted Payouts (Tab 1)
const filteredPayouts = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()
  let list = payouts.value

  if (q) {
    list = list.filter((p) => {
      const matchShop = p.shopName.toLowerCase().includes(q)
      const matchBank = (p.bankAccountNumber || '').includes(q)
      const matchPromptPay = (p.promptPay || '').includes(q)
      const matchHolder = (p.bankAccountName || '').toLowerCase().includes(q)
      const matchOrder = p.orders.some((o) => o.orderNumber.toLowerCase().includes(q))
      return matchShop || matchBank || matchPromptPay || matchHolder || matchOrder
    })
  }

  return [...list].sort((a, b) => {
    if (sortBy.value === 'amount_desc') {
      return (b.totalSalesAmount || 0) - (a.totalSalesAmount || 0)
    }
    if (sortBy.value === 'amount_asc') {
      return (a.totalSalesAmount || 0) - (b.totalSalesAmount || 0)
    }
    if (sortBy.value === 'orders_desc') {
      return (b.paidOrdersCount || 0) - (a.paidOrdersCount || 0)
    }
    return 0
  })
})

// Filtered and Sorted History (Tab 2)
const filteredHistory = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()
  let list = history.value

  if (q) {
    list = list.filter((h) => {
      const matchShop = (h.shopName || '').toLowerCase().includes(q)
      const matchRef = (h.transactionRef || '').toLowerCase().includes(q)
      const matchNote = (h.note || '').toLowerCase().includes(q)
      const matchId = h.payoutId.toLowerCase().includes(q)
      return matchShop || matchRef || matchNote || matchId
    })
  }

  return [...list].sort((a, b) => {
    const timeA = new Date(a.createdAt).getTime()
    const timeB = new Date(b.createdAt).getTime()
    return timeB - timeA
  })
})

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

function openPayoutModal(shop: AdminMerchantPayoutGroup) {
  selectedShopForPayout.value = shop
  payoutFile.value = null
  payoutPreviewUrl.value = ''
  transactionRef.value = ''
  note.value = `โอนเงินยอดขายร้าน ${shop.shopName}`
}

function closePayoutModal() {
  selectedShopForPayout.value = null
  if (payoutPreviewUrl.value) URL.revokeObjectURL(payoutPreviewUrl.value)
  payoutFile.value = null
  payoutPreviewUrl.value = ''
}

function handleFileChange(event: Event) {
  const target = event.target as HTMLInputElement
  if (target.files && target.files[0]) {
    setFile(target.files[0])
  }
}

function handleDrop(event: DragEvent) {
  if (event.dataTransfer?.files && event.dataTransfer.files[0]) {
    setFile(event.dataTransfer.files[0])
  }
}

function setFile(file: File) {
  if (!file.type.startsWith('image/')) {
    void swal.error('ไฟล์ไม่ถูกต้อง', 'กรุณาอัปโหลดไฟล์รูปภาพเท่านั้น (.jpg, .png, .jpeg, .webp)')
    return
  }
  payoutFile.value = file
  payoutPreviewUrl.value = URL.createObjectURL(file)
}

async function submitPayout() {
  if (!selectedShopForPayout.value) return
  submittingPayout.value = true

  try {
    const formData = new FormData()
    formData.append('shopId', selectedShopForPayout.value.shopId)
    formData.append(
      'orderIds',
      selectedShopForPayout.value.orders.map((o) => o.orderId).join(','),
    )
    if (transactionRef.value) formData.append('transactionRef', transactionRef.value)
    if (note.value) formData.append('note', note.value)
    if (payoutFile.value) formData.append('slipFile', payoutFile.value)

    await http.post('/orders/admin/payouts/confirm', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })

    await swal.success(
      'บันทึกการโอนเงินสำเร็จ',
      'ระบบได้บันทึกการสั่งจ่ายและส่งหลักฐานแจ้งเตือนไปยังร้านค้าเรียบร้อยแล้ว',
    )
    closePayoutModal()
    await loadData()
  } catch (error) {
    await swal.error('บันทึกการโอนเงินไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    submittingPayout.value = false
  }
}

// Lightbox modal helpers
function openSlipLightbox(url: string) {
  selectedSlipUrl.value = url
  zoomScale.value = 1
  rotation.value = 0
}

function closeSlipLightbox() {
  selectedSlipUrl.value = null
  zoomScale.value = 1
  rotation.value = 0
}

function zoomIn() {
  zoomScale.value = Math.min(zoomScale.value + 0.25, 3)
}

function zoomOut() {
  zoomScale.value = Math.max(zoomScale.value - 0.25, 0.5)
}

function resetZoom() {
  zoomScale.value = 1
  rotation.value = 0
}

function rotateSlip() {
  rotation.value = (rotation.value + 90) % 360
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') {
    if (selectedSlipUrl.value) closeSlipLightbox()
    else if (selectedShopForPayout.value) closePayoutModal()
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
    <!-- Top Header Banner -->
    <div class="flex flex-wrap items-center justify-between gap-4 border-b border-slate-300/80 pb-6">
      <div>
        <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-teal-50 border border-teal-200 text-xs font-bold text-teal-800 mb-2">
          <i class="mdi mdi-cash-refund text-teal-700"></i>
          <span>ระบบสั่งจ่ายและเคลียร์ยอดขายร้านค้า</span>
        </div>
        <h1 class="text-2xl sm:text-3xl font-black tracking-tight text-slate-900">
          การโอนเงินเคลียร์ยอดขายให้ร้านค้า (Settlement)
        </h1>
        <p class="mt-1 text-xs sm:text-sm text-slate-600 font-bold">
          ตรวจสอบยอดขายสะสมของแต่ละร้านค้า ดำเนินการโอนเงิน แนบหลักฐานสลิป และตรวจสอบประวัติการทำรายการย้อนหลัง
        </p>
      </div>

      <div class="flex items-center gap-3">
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-xl bg-white hover:bg-slate-50 border border-slate-300 px-4 py-2.5 text-xs font-bold text-slate-700 shadow-xs transition active:scale-95 cursor-pointer hover:border-slate-300"
          :disabled="loading || historyLoading"
          @click="loadData"
        >
          <i
            class="mdi mdi-refresh text-base text-teal-700"
            :class="{ 'animate-spin': loading || historyLoading }"
          ></i>
          <span>รีเฟรชข้อมูล</span>
        </button>
      </div>
    </div>

    <!-- KPI Metric Cards Grid -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <!-- Stat 1: Total Pending Payout Amount -->
      <div
        class="rounded-2xl border border-slate-300 bg-white p-5 shadow-xs flex items-center justify-between cursor-pointer hover:border-teal-300 transition"
        :class="{ 'ring-2 ring-teal-600/20 border-teal-300': activeTab === 'pending' }"
        @click="selectTab('pending')"
      >
        <div>
          <span class="text-xs font-bold text-slate-600 uppercase tracking-wider block">ยอดรอสั่งจ่ายรวม</span>
          <div class="mt-1.5">
            <span class="text-2xl sm:text-3xl font-black text-teal-700">{{ formatCurrency(totalPendingAmount) }}</span>
          </div>
        </div>
        <div class="h-12 w-12 rounded-2xl bg-teal-50 text-teal-700 flex items-center justify-center text-2xl border border-teal-100">
          <i class="mdi mdi-cash-clock"></i>
        </div>
      </div>

      <!-- Stat 2: Pending Shops -->
      <div
        class="rounded-2xl border border-slate-300 bg-white p-5 shadow-xs flex items-center justify-between cursor-pointer hover:border-slate-300 transition"
        @click="selectTab('pending')"
      >
        <div>
          <span class="text-xs font-bold text-slate-600 uppercase tracking-wider block">ร้านค้าที่รอรับเงิน</span>
          <div class="mt-1.5 flex items-baseline gap-2">
            <span class="text-2xl sm:text-3xl font-black text-slate-900">{{ payouts.length }}</span>
            <span class="text-xs font-bold text-amber-700 bg-amber-50 border border-amber-200 px-2 py-0.5 rounded-full">
              รอดำเนินการ
            </span>
          </div>
        </div>
        <div class="h-12 w-12 rounded-2xl bg-amber-50 text-amber-700 flex items-center justify-center text-2xl border border-amber-200">
          <i class="mdi mdi-store-clock-outline"></i>
        </div>
      </div>

      <!-- Stat 3: Orders to Clear -->
      <div
        class="rounded-2xl border border-slate-300 bg-white p-5 shadow-xs flex items-center justify-between cursor-pointer hover:border-slate-300 transition"
        @click="selectTab('pending')"
      >
        <div>
          <span class="text-xs font-bold text-slate-600 uppercase tracking-wider block">คำสั่งซื้อรอเคลียร์ยอด</span>
          <div class="mt-1.5 flex items-baseline gap-2">
            <span class="text-2xl sm:text-3xl font-black text-slate-900">{{ totalPendingOrders }}</span>
            <span class="text-xs font-bold text-slate-600">ออเดอร์</span>
          </div>
        </div>
        <div class="h-12 w-12 rounded-2xl bg-sky-50 text-sky-700 flex items-center justify-center text-2xl border border-sky-200">
          <i class="mdi mdi-package-variant-closed"></i>
        </div>
      </div>

      <!-- Stat 4: Settled History Amount -->
      <div
        class="rounded-2xl border border-slate-300 bg-white p-5 shadow-xs flex items-center justify-between cursor-pointer hover:border-emerald-300 transition"
        :class="{ 'ring-2 ring-emerald-500/20 border-emerald-300': activeTab === 'history' }"
        @click="selectTab('history')"
      >
        <div>
          <span class="text-xs font-bold text-slate-600 uppercase tracking-wider block">ประวัติการโอนแล้ว</span>
          <div class="mt-1.5 flex items-baseline gap-2">
            <span class="text-2xl sm:text-3xl font-black text-emerald-700">{{ history.length }}</span>
            <span class="text-xs font-bold text-slate-600">ครั้ง</span>
          </div>
        </div>
        <div class="h-12 w-12 rounded-2xl bg-emerald-50 text-emerald-700 flex items-center justify-center text-2xl border border-emerald-200">
          <i class="mdi mdi-check-decagram-outline"></i>
        </div>
      </div>
    </div>

    <!-- Tab Bar & Search Toolbar -->
    <div class="rounded-2xl border border-slate-300 bg-white p-4 shadow-xs space-y-4">
      <div class="flex flex-wrap items-center justify-between gap-4 border-b border-slate-200 pb-3">
        <!-- Tab Toggle Buttons -->
        <div class="inline-flex rounded-xl bg-slate-100 p-1 border border-slate-300">
          <button
            type="button"
            class="px-4 py-2 rounded-lg text-xs font-bold transition cursor-pointer flex items-center gap-2"
            :class="activeTab === 'pending' ? 'bg-teal-700 text-white shadow-xs' : 'text-slate-700 hover:text-slate-900'"
            @click="selectTab('pending')"
          >
            <i class="mdi mdi-clock-outline"></i>
            <span>ยอดค้างโอนให้ร้านค้า</span>
            <span
              class="px-2 py-0.5 rounded-full text-[10px] font-bold"
              :class="activeTab === 'pending' ? 'bg-white/20 text-white' : 'bg-slate-200 text-slate-700'"
            >
              {{ payouts.length }}
            </span>
          </button>

          <button
            type="button"
            class="px-4 py-2 rounded-lg text-xs font-bold transition cursor-pointer flex items-center gap-2"
            :class="activeTab === 'history' ? 'bg-slate-800 text-white shadow-xs' : 'text-slate-700 hover:text-slate-900'"
            @click="selectTab('history')"
          >
            <i class="mdi mdi-history"></i>
            <span>ประวัติการสั่งจ่ายแล้ว</span>
            <span
              class="px-2 py-0.5 rounded-full text-[10px] font-bold"
              :class="activeTab === 'history' ? 'bg-white/20 text-white' : 'bg-slate-200 text-slate-700'"
            >
              {{ history.length }}
            </span>
          </button>
        </div>

        <!-- Sort Select (Contextual) -->
        <div v-if="activeTab === 'pending'" class="flex items-center gap-2">
          <label class="text-xs font-bold text-slate-600 shrink-0">เรียงตาม:</label>
          <select
            v-model="sortBy"
            class="rounded-xl border border-slate-300 bg-white px-3 py-2 text-xs font-bold text-slate-700 outline-none focus:border-teal-600 cursor-pointer shadow-2xs"
          >
            <option value="amount_desc">ยอดเงินสูงสุด</option>
            <option value="amount_asc">ยอดเงินต่ำสุด</option>
            <option value="orders_desc">จำนวนออเดอร์มากสุด</option>
          </select>
        </div>
      </div>

      <!-- Search Input -->
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div class="relative w-full max-w-md">
          <i class="mdi mdi-magnify absolute left-3.5 top-1/2 -translate-y-1/2 text-lg text-slate-500"></i>
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="
              activeTab === 'pending'
                ? 'ค้นหาชื่อร้านค้า, บัญชีธนาคาร, พร้อมเพย์ หรือเลขออเดอร์...'
                : 'ค้นหาชื่อร้านค้า, รหัสอ้างอิงธุรกรรม หรือหมายเหตุ...'
            "
            class="w-full rounded-xl border border-slate-300 bg-slate-50/60 py-2.5 pl-10 pr-4 text-xs font-bold text-slate-800 placeholder-slate-400 focus:bg-white focus:outline-none focus:ring-2 focus:ring-teal-600/20 focus:border-teal-600 transition"
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

        <div class="text-xs font-bold text-slate-600">
          แสดง {{ activeTab === 'pending' ? filteredPayouts.length : filteredHistory.length }} รายการ
        </div>
      </div>
    </div>

    <!-- Skeleton Loading -->
    <div v-if="loading || (activeTab === 'history' && historyLoading)" class="space-y-4">
      <div v-for="i in 3" :key="i" class="h-44 animate-pulse rounded-2xl bg-white border border-slate-300 shadow-xs"></div>
    </div>

    <!-- TAB 1: PENDING PAYOUTS LIST -->
    <template v-else-if="activeTab === 'pending'">
      <!-- Empty State: Zero pending -->
      <div
        v-if="payouts.length === 0"
        class="rounded-3xl border border-slate-300 bg-white p-14 text-center space-y-4 shadow-xs"
      >
        <div class="h-20 w-20 rounded-3xl bg-emerald-50 text-emerald-600 flex items-center justify-center text-4xl mx-auto shadow-xs border border-emerald-100">
          <i class="mdi mdi-check-decagram-outline"></i>
        </div>
        <div class="max-w-md mx-auto space-y-1.5">
          <h3 class="text-xl font-black text-slate-900">ไม่มีรายการยอดขายที่ค้างโอน</h3>
          <p class="text-xs text-slate-600 font-bold">
            ยอดขายของร้านค้าทั้งหมดได้รับการสั่งจ่ายและโอนเงินครบถ้วนเรียบร้อยแล้ว
          </p>
        </div>
      </div>

      <!-- Empty State: Filter produced no matches -->
      <div
        v-else-if="filteredPayouts.length === 0"
        class="rounded-3xl border border-slate-300 bg-white p-12 text-center space-y-3 shadow-xs"
      >
        <div class="h-14 w-14 rounded-2xl bg-slate-100 text-slate-500 flex items-center justify-center text-2xl mx-auto">
          <i class="mdi mdi-file-search-outline"></i>
        </div>
        <h3 class="text-base font-bold text-slate-900">ไม่พบร้านค้าที่ตรงกับคำค้นหา</h3>
        <p class="text-xs text-slate-600 font-bold">ลองเปลี่ยนคำค้นหา หรือรีเซ็ตการค้นหา</p>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 text-xs font-bold text-teal-700 hover:underline cursor-pointer pt-1"
          @click="searchQuery = ''"
        >
          <i class="mdi mdi-backspace-outline"></i> ล้างการค้นหา
        </button>
      </div>

      <!-- Pending Shops Card List -->
      <div v-else class="space-y-6">
        <div
          v-for="shop in filteredPayouts"
          :key="shop.shopId"
          class="rounded-2xl border border-slate-300 bg-white shadow-xs hover:border-slate-300 transition overflow-hidden"
        >
          <!-- Card Top Bar -->
          <div class="flex flex-wrap items-center justify-between gap-4 p-5 sm:p-6 border-b border-slate-200 bg-slate-50/50">
            <div class="flex items-center gap-3.5">
              <img
                v-if="shop.shopLogoUrl"
                :src="imageUrl(shop.shopLogoUrl)"
                :alt="shop.shopName"
                class="h-14 w-14 rounded-2xl object-cover border border-slate-300 shrink-0"
              />
              <div
                v-else
                class="flex h-14 w-14 items-center justify-center rounded-2xl bg-teal-700 text-white font-bold text-2xl shrink-0 shadow-xs"
              >
                <i class="mdi mdi-store"></i>
              </div>

              <div>
                <div class="flex flex-wrap items-center gap-2">
                  <h3 class="text-base font-black text-slate-900">{{ shop.shopName }}</h3>
                  <button
                    type="button"
                    class="inline-flex items-center gap-1 text-[11px] font-mono text-slate-600 hover:text-teal-700 bg-white border border-slate-300 px-2 py-0.5 rounded-lg cursor-pointer transition"
                    title="คัดลอกรหัสร้านค้า"
                    @click="copyToClipboard(shop.shopId, `shop-${shop.shopId}`)"
                  >
                    <span>#{{ shop.shopId.slice(0, 8) }}</span>
                    <i
                      class="mdi text-[10px]"
                      :class="copiedId === `shop-${shop.shopId}` ? 'mdi-check text-emerald-600' : 'mdi-content-copy text-slate-500'"
                    ></i>
                  </button>
                </div>
                <p class="text-xs text-slate-600 font-bold mt-1 flex items-center gap-1.5">
                  <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-slate-100 text-slate-700 font-bold text-[11px]">
                    <i class="mdi mdi-receipt-text-outline"></i>
                    {{ shop.paidOrdersCount }} คำสั่งซื้อพร้อมสั่งจ่าย
                  </span>
                </p>
              </div>
            </div>

            <!-- Total Sales Amount & Payout Action Button -->
            <div class="flex flex-wrap items-center gap-4">
              <div class="text-right">
                <span class="text-[10px] font-bold text-slate-600 uppercase tracking-wider block">
                  ยอดรวมที่ต้องสั่งจ่าย
                </span>
                <span class="text-2xl font-black text-teal-700">
                  {{ formatCurrency(shop.totalSalesAmount) }}
                </span>
              </div>

              <button
                type="button"
                class="px-5 py-3 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs shadow-xs transition active:scale-95 cursor-pointer border border-emerald-600 flex items-center gap-2"
                @click="openPayoutModal(shop)"
              >
                <i class="mdi mdi-bank-transfer text-lg text-white"></i>
                <span class="!text-white font-bold">แจ้งโอนเงินให้ร้านค้า</span>
              </button>
            </div>
          </div>

          <!-- Card Body: Bank Details (Left 4 cols) & Orders Breakdown (Right 8 cols) -->
          <div class="p-5 sm:p-6 grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
            <!-- Left: Bank Account Card (4 cols) -->
            <div class="lg:col-span-4 rounded-2xl border border-slate-300 bg-slate-50/60 p-5 space-y-4 shadow-2xs">
              <div class="flex items-center justify-between border-b border-slate-300 pb-3">
                <span class="text-xs font-bold text-slate-900 flex items-center gap-1.5">
                  <i class="mdi mdi-bank text-emerald-600 text-base"></i> ข้อมูลบัญชีสำหรับโอนเงิน
                </span>
                <span
                  v-if="shop.bankAccountNumber || shop.promptPay"
                  class="text-[10px] font-bold text-emerald-700 bg-emerald-50 border border-emerald-200 px-2 py-0.5 rounded-full"
                >
                  พร้อมโอน
                </span>
              </div>

              <!-- Warning if bank info missing -->
              <div
                v-if="!shop.bankName && !shop.bankAccountNumber && !shop.promptPay"
                class="py-3 px-3.5 text-xs text-amber-800 font-bold bg-amber-50 rounded-xl border border-amber-200 flex items-start gap-2"
              >
                <i class="mdi mdi-alert-circle-outline text-amber-600 text-base shrink-0 mt-0.5"></i>
                <div>
                  <p class="font-bold">ยังไม่มีข้อมูลบัญชี</p>
                  <p class="text-[11px] text-amber-700 font-bold">ร้านค้านี้ยังไม่ได้บันทึกข้อมูลบัญชีธนาคารในระบบ</p>
                </div>
              </div>

              <!-- Bank details fields -->
              <div v-else class="space-y-3 text-xs">
                <div class="flex items-center justify-between">
                  <span class="text-slate-600 font-bold">ธนาคาร:</span>
                  <strong class="text-slate-800">{{ shop.bankName || '-' }}</strong>
                </div>

                <div class="flex items-center justify-between">
                  <span class="text-slate-600 font-bold">ชื่อบัญชี:</span>
                  <strong class="text-slate-800">{{ shop.bankAccountName || '-' }}</strong>
                </div>

                <!-- Account Number with Copy Button -->
                <div
                  v-if="shop.bankAccountNumber"
                  class="p-3 rounded-xl bg-white border border-slate-300 flex items-center justify-between shadow-2xs"
                >
                  <div>
                    <span class="text-[10px] text-slate-600 block font-bold">เลขที่บัญชี:</span>
                    <span class="font-mono text-sm font-bold text-teal-800">{{ shop.bankAccountNumber }}</span>
                  </div>
                  <button
                    type="button"
                    class="px-2.5 py-1 rounded-lg bg-teal-700 hover:bg-teal-800 text-white text-[11px] font-bold transition cursor-pointer active:scale-95 shadow-2xs flex items-center gap-1"
                    title="คัดลอกเลขบัญชี"
                    @click="copyToClipboard(shop.bankAccountNumber!, `acc-${shop.shopId}`)"
                  >
                    <i
                      class="mdi"
                      :class="copiedId === `acc-${shop.shopId}` ? 'mdi-check' : 'mdi-content-copy'"
                    ></i>
                    <span>{{ copiedId === `acc-${shop.shopId}` ? 'คัดลอกแล้ว' : 'คัดลอก' }}</span>
                  </button>
                </div>

                <!-- PromptPay with Copy Button -->
                <div
                  v-if="shop.promptPay"
                  class="p-3 rounded-xl bg-white border border-slate-300 flex items-center justify-between shadow-2xs"
                >
                  <div>
                    <span class="text-[10px] text-slate-600 block font-bold">พร้อมเพย์ (PromptPay):</span>
                    <span class="font-mono text-xs font-bold text-slate-900">{{ shop.promptPay }}</span>
                  </div>
                  <button
                    type="button"
                    class="px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-900 text-white text-[11px] font-bold transition cursor-pointer active:scale-95 shadow-2xs flex items-center gap-1"
                    title="คัดลอกเบอร์พร้อมเพย์"
                    @click="copyToClipboard(shop.promptPay!, `pp-${shop.shopId}`)"
                  >
                    <i
                      class="mdi"
                      :class="copiedId === `pp-${shop.shopId}` ? 'mdi-check' : 'mdi-content-copy'"
                    ></i>
                    <span>{{ copiedId === `pp-${shop.shopId}` ? 'คัดลอกแล้ว' : 'คัดลอก' }}</span>
                  </button>
                </div>
              </div>
            </div>

            <!-- Right: Included Orders Breakdown (8 cols) -->
            <div class="lg:col-span-8 space-y-3">
              <div class="flex items-center justify-between">
                <h4 class="text-xs font-bold uppercase text-slate-600 tracking-wider flex items-center gap-1.5">
                  <i class="mdi mdi-receipt-text-outline text-teal-700"></i>
                  <span>รายการคำสั่งซื้อที่รวมอยู่ในยอดนี้ ({{ shop.orders.length }} รายการ)</span>
                </h4>
                <span class="text-xs font-bold text-slate-600">
                  รวม {{ formatCurrency(shop.totalSalesAmount) }}
                </span>
              </div>

              <!-- Orders List with Scroll Container -->
              <div class="space-y-2.5 max-h-72 overflow-y-auto pr-1">
                <div
                  v-for="ord in shop.orders"
                  :key="ord.orderId"
                  class="rounded-xl border border-slate-300 bg-white p-3.5 space-y-2 shadow-2xs hover:border-slate-300 transition"
                >
                  <div class="flex items-center justify-between text-xs border-b border-slate-200 pb-2">
                    <div class="flex items-center gap-2">
                      <button
                        type="button"
                        class="inline-flex items-center gap-1 font-mono font-bold text-slate-800 hover:text-teal-700 cursor-pointer"
                        @click="copyToClipboard(ord.orderNumber, ord.orderId)"
                      >
                        <span>#{{ ord.orderNumber }}</span>
                        <i
                          class="mdi text-[10px]"
                          :class="copiedId === ord.orderId ? 'mdi-check text-emerald-600' : 'mdi-content-copy text-slate-500'"
                        ></i>
                      </button>
                      <span class="text-slate-500">•</span>
                      <span class="text-slate-600 text-[11px]">
                        {{ formatDate(ord.createdAt, { day: 'numeric', month: 'short', year: '2-digit', hour: '2-digit', minute: '2-digit' }) }}
                      </span>
                    </div>

                    <span class="font-bold text-teal-700 text-sm">
                      {{ formatCurrency(ord.totalAmount) }}
                    </span>
                  </div>

                  <!-- Product Items preview -->
                  <div class="space-y-1 pt-0.5">
                    <div
                      v-for="item in ord.items"
                      :key="item.orderItemId"
                      class="flex items-center justify-between text-[11px] text-slate-700"
                    >
                      <div class="flex items-center gap-1.5 truncate pr-2">
                        <span class="h-1 w-1 rounded-full bg-slate-400 shrink-0"></span>
                        <span class="truncate">{{ item.productName }}</span>
                        <span class="text-slate-500 shrink-0">x{{ item.quantity }}</span>
                      </div>
                      <span class="font-bold text-slate-700 shrink-0">{{ formatCurrency(item.totalPrice) }}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- TAB 2: PAYOUT HISTORY LIST -->
    <template v-else-if="activeTab === 'history'">
      <!-- Empty State -->
      <div
        v-if="history.length === 0"
        class="rounded-3xl border border-slate-300 bg-white p-14 text-center space-y-4 shadow-xs"
      >
        <div class="h-20 w-20 rounded-3xl bg-slate-100 text-slate-500 flex items-center justify-center text-4xl mx-auto shadow-xs border border-slate-300">
          <i class="mdi mdi-history"></i>
        </div>
        <div class="max-w-md mx-auto space-y-1.5">
          <h3 class="text-xl font-black text-slate-900">ยังไม่มีประวัติการโอนเงิน</h3>
          <p class="text-xs text-slate-600 font-bold">
            เมื่อมีการยืนยันการโอนเงินเคลียร์ยอดขาย รายการจะถูกบันทึกประวัติไว้ที่นี่
          </p>
        </div>
      </div>

      <!-- Empty State: Filter produced no matches -->
      <div
        v-else-if="filteredHistory.length === 0"
        class="rounded-3xl border border-slate-300 bg-white p-12 text-center space-y-3 shadow-xs"
      >
        <div class="h-14 w-14 rounded-2xl bg-slate-100 text-slate-500 flex items-center justify-center text-2xl mx-auto">
          <i class="mdi mdi-file-search-outline"></i>
        </div>
        <h3 class="text-base font-bold text-slate-900">ไม่พบประวัติที่ตรงกับคำค้นหา</h3>
        <p class="text-xs text-slate-600 font-bold">ลองเปลี่ยนคำค้นหา หรือรีเซ็ตการค้นหา</p>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 text-xs font-bold text-teal-700 hover:underline cursor-pointer pt-1"
          @click="searchQuery = ''"
        >
          <i class="mdi mdi-backspace-outline"></i> ล้างการค้นหา
        </button>
      </div>

      <!-- History Records List -->
      <div v-else class="space-y-4">
        <div
          v-for="record in filteredHistory"
          :key="record.payoutId"
          class="rounded-2xl border border-slate-300 bg-white p-5 shadow-xs flex flex-wrap items-center justify-between gap-4 hover:border-slate-300 transition"
        >
          <div class="flex items-center gap-3.5">
            <div class="h-12 w-12 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center text-xl font-bold shrink-0 border border-emerald-100">
              <i class="mdi mdi-check-circle-outline"></i>
            </div>
            <div>
              <div class="flex flex-wrap items-center gap-2">
                <h3 class="font-bold text-base text-slate-900">{{ record.shopName || 'ร้านค้าชุมชน' }}</h3>
                <span class="px-2.5 py-0.5 rounded-full bg-emerald-50 text-emerald-700 text-[10px] font-bold border border-emerald-200">
                  โอนแล้ว
                </span>
                <span
                  v-if="record.transactionRef"
                  class="text-[11px] font-mono text-slate-600 bg-slate-100 border border-slate-300 px-2 py-0.5 rounded-md"
                >
                  Ref: {{ record.transactionRef }}
                </span>
              </div>
              <p class="text-xs text-slate-600 font-bold mt-1">
                โอนเมื่อ: {{ formatDate(record.createdAt, { day: 'numeric', month: 'short', year: '2-digit', hour: '2-digit', minute: '2-digit' }) }}
                <span class="text-slate-300">•</span>
                รวม {{ record.ordersCount }} คำสั่งซื้อ
              </p>
              <p v-if="record.note" class="text-xs text-teal-700 font-bold mt-0.5">
                หมายเหตุ: {{ record.note }}
              </p>
            </div>
          </div>

          <div class="flex items-center gap-4">
            <span class="text-xl font-black text-teal-700">{{ formatCurrency(record.totalAmount) }}</span>
            <button
              v-if="record.slipImageUrl"
              type="button"
              class="px-3.5 py-2 rounded-xl bg-white border border-slate-300 hover:border-teal-400 text-xs font-bold text-slate-700 transition cursor-pointer flex items-center gap-1.5 shadow-2xs"
              @click="openSlipLightbox(record.slipImageUrl)"
            >
              <i class="mdi mdi-eye-outline text-base text-teal-700"></i>
              <span>ดูสลิปโอนเงิน</span>
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- MODAL FOR CONFIRMING PAYOUT WITH SLIP -->
    <div
      v-if="selectedShopForPayout"
      class="fixed inset-0 z-50 bg-black/75 flex items-center justify-center p-4 backdrop-blur-xs"
      @click="closePayoutModal"
    >
      <div
        class="relative w-full max-w-xl bg-white border border-slate-300 rounded-3xl p-6 sm:p-8 shadow-2xl space-y-6"
        @click.stop
      >
        <!-- Modal Top Bar -->
        <div class="flex items-center justify-between border-b border-slate-200 pb-4">
          <div class="flex items-center gap-3">
            <div class="h-10 w-10 rounded-xl bg-emerald-600 text-white flex items-center justify-center font-bold text-lg shadow-xs">
              <i class="mdi mdi-bank-transfer"></i>
            </div>
            <div>
              <h2 class="text-lg font-bold text-slate-900">แจ้งโอนเงินให้ร้านค้า</h2>
              <p class="text-xs text-slate-600 font-bold">{{ selectedShopForPayout.shopName }}</p>
            </div>
          </div>
          <button
            type="button"
            class="text-slate-500 hover:text-slate-700 text-2xl font-bold p-1 cursor-pointer"
            @click="closePayoutModal"
          >
            <i class="mdi mdi-close"></i>
          </button>
        </div>

        <!-- Settlement Summary Banner -->
        <div class="rounded-2xl bg-slate-50 p-4 space-y-2.5 border border-slate-300 text-xs">
          <div class="flex justify-between font-bold text-slate-800">
            <span>ยอดขายรวมที่ต้องโอน:</span>
            <span class="text-teal-700 text-base font-black">
              {{ formatCurrency(selectedShopForPayout.totalSalesAmount) }}
            </span>
          </div>
          <div class="flex justify-between text-slate-600">
            <span>จำนวนคำสั่งซื้อในรายการนี้:</span>
            <span class="font-bold text-slate-800">{{ selectedShopForPayout.paidOrdersCount }} รายการ</span>
          </div>
          <div
            v-if="selectedShopForPayout.bankAccountNumber"
            class="flex justify-between text-slate-600 pt-1.5 border-t border-slate-300"
          >
            <span>โอนเข้าบัญชี:</span>
            <span class="font-bold text-slate-800">
              {{ selectedShopForPayout.bankName }} - {{ selectedShopForPayout.bankAccountNumber }}
              ({{ selectedShopForPayout.bankAccountName }})
            </span>
          </div>
        </div>

        <!-- File Upload Drag & Drop Zone -->
        <div class="space-y-2">
          <label class="block text-xs font-bold text-slate-800">
            แนบรูปภาพสลิปการโอนเงิน (Payout Slip)
          </label>

          <div
            v-if="!payoutPreviewUrl"
            class="relative border-2 border-dashed border-slate-300 hover:border-teal-600 bg-slate-50/50 rounded-2xl p-6 text-center cursor-pointer transition group"
            @dragover.prevent
            @drop.prevent="handleDrop"
          >
            <input
              type="file"
              accept="image/*"
              class="absolute inset-0 opacity-0 cursor-pointer w-full h-full z-10"
              @change="handleFileChange"
            />
            <div class="space-y-2 pointer-events-none">
              <div class="h-10 w-10 rounded-xl bg-teal-50 text-teal-700 mx-auto flex items-center justify-center text-xl group-hover:scale-110 transition border border-teal-100">
                <i class="mdi mdi-cloud-upload-outline"></i>
              </div>
              <p class="text-xs font-bold text-slate-800">คลิก หรือ ลากสลิปมาวางที่นี่</p>
              <p class="text-[10px] text-slate-600">รองรับไฟล์รูปภาพ .JPG, .PNG, .WEBP</p>
            </div>
          </div>

          <div v-else class="relative rounded-2xl border-2 border-teal-600 overflow-hidden bg-slate-100 p-2 space-y-2">
            <div class="relative aspect-3/4 max-h-48 mx-auto rounded-xl overflow-hidden shadow-xs bg-white">
              <img :src="payoutPreviewUrl" alt="สลิปโอนเงิน" class="w-full h-full object-contain" />
            </div>
            <div class="flex items-center justify-between px-1">
              <span class="text-xs font-bold text-emerald-700 flex items-center gap-1">
                <i class="mdi mdi-check-circle"></i> แนบสลิปเรียบร้อย
              </span>
              <button
                type="button"
                class="text-xs text-rose-600 font-bold underline cursor-pointer"
                @click="payoutFile = null; payoutPreviewUrl = ''"
              >
                เปลี่ยนรูปสลิป
              </button>
            </div>
          </div>
        </div>

        <div class="space-y-3 text-xs">
          <div>
            <label class="block font-bold text-slate-800 mb-1">หมายเลขอ้างอิงธุรกรรม (Transaction Ref)</label>
            <input
              v-model="transactionRef"
              type="text"
              placeholder="เช่น TXN-20261005-001"
              class="w-full rounded-xl border border-slate-300 bg-white px-4 py-2.5 font-bold text-slate-900 focus:outline-none focus:border-teal-600"
            />
          </div>

          <div>
            <label class="block font-bold text-slate-800 mb-1">หมายเหตุเพิ่มเติมถึงร้านค้า</label>
            <input
              v-model="note"
              type="text"
              placeholder="เช่น ยอดโอนสุทธิเคลียร์ยอดขายประจำรอบ"
              class="w-full rounded-xl border border-slate-300 bg-white px-4 py-2.5 font-bold text-slate-900 focus:outline-none focus:border-teal-600"
            />
          </div>
        </div>

        <div class="flex items-center justify-end gap-3 pt-2">
          <button
            type="button"
            class="px-5 py-2.5 rounded-xl border border-slate-300 text-xs font-bold text-slate-700 hover:bg-slate-50 transition cursor-pointer"
            @click="closePayoutModal"
          >
            ยกเลิก
          </button>
          <button
            type="button"
            class="px-6 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold transition active:scale-95 shadow-xs border border-emerald-600 cursor-pointer flex items-center gap-2"
            :disabled="submittingPayout"
            @click="submitPayout"
          >
            <i
              class="mdi text-base text-white"
              :class="submittingPayout ? 'mdi-loading animate-spin' : 'mdi-check-circle'"
            ></i>
            <span class="!text-white font-bold">
              {{ submittingPayout ? 'กำลังบันทึก...' : 'ยืนยันโอนเงินและแจ้งร้านค้า' }}
            </span>
          </button>
        </div>
      </div>
    </div>

    <!-- Enhanced Lightbox Modal for Zooming / Inspecting Historical Slip -->
    <div
      v-if="selectedSlipUrl"
      class="fixed inset-0 z-50 bg-black/85 flex items-center justify-center p-3 sm:p-6 backdrop-blur-sm"
      @click="closeSlipLightbox"
    >
      <div
        class="relative w-full max-w-3xl max-h-[95vh] bg-white rounded-3xl shadow-2xl flex flex-col overflow-hidden border border-slate-300"
        @click.stop
      >
        <!-- Modal Top Bar -->
        <div class="flex items-center justify-between px-6 py-4 border-b border-slate-200 bg-slate-50/80">
          <div class="flex items-center gap-3">
            <div class="h-9 w-9 rounded-xl bg-teal-50 text-teal-700 flex items-center justify-center font-bold text-lg border border-teal-100">
              <i class="mdi mdi-receipt-text-check"></i>
            </div>
            <div>
              <h3 class="text-sm font-black text-slate-900">หลักฐานสลิปการโอนเงินเคลียร์ยอดขาย</h3>
              <p class="text-[11px] text-slate-600 font-bold">สลิปการสั่งจ่ายที่ส่งให้ร้านค้า</p>
            </div>
          </div>

          <!-- Controls: Zoom In / Zoom Out / Rotate / Close -->
          <div class="flex items-center gap-2">
            <button
              type="button"
              class="h-8 w-8 rounded-lg bg-white border border-slate-300 hover:bg-slate-100 text-slate-700 flex items-center justify-center text-sm cursor-pointer shadow-2xs"
              title="ย่อขนาด"
              @click="zoomOut"
            >
              <i class="mdi mdi-magnify-minus-outline"></i>
            </button>
            <button
              type="button"
              class="h-8 w-8 rounded-lg bg-white border border-slate-300 hover:bg-slate-100 text-slate-700 flex items-center justify-center text-sm cursor-pointer shadow-2xs"
              title="ขยายขนาด"
              @click="zoomIn"
            >
              <i class="mdi mdi-magnify-plus-outline"></i>
            </button>
            <button
              type="button"
              class="h-8 w-8 rounded-lg bg-white border border-slate-300 hover:bg-slate-100 text-slate-700 flex items-center justify-center text-sm cursor-pointer shadow-2xs"
              title="หมุนรูปภาพ 90 องศา"
              @click="rotateSlip"
            >
              <i class="mdi mdi-rotate-right"></i>
            </button>
            <button
              type="button"
              class="h-8 w-8 rounded-lg bg-white border border-slate-300 hover:bg-slate-100 text-slate-700 flex items-center justify-center text-xs font-bold cursor-pointer shadow-2xs"
              title="รีเซ็ตขนาด"
              @click="resetZoom"
            >
              100%
            </button>
            <button
              type="button"
              class="h-8 w-8 rounded-lg bg-rose-50 hover:bg-rose-100 text-rose-600 flex items-center justify-center text-base cursor-pointer ml-1"
              title="ปิด (Esc)"
              @click="closeSlipLightbox"
            >
              <i class="mdi mdi-close"></i>
            </button>
          </div>
        </div>

        <!-- Modal Image Canvas -->
        <div class="flex-1 overflow-auto p-4 sm:p-6 bg-slate-900/90 flex items-center justify-center min-h-[400px]">
          <img
            :src="selectedSlipUrl"
            alt="สลิปโอนเงินแบบขยาย"
            class="max-h-[65vh] w-auto mx-auto object-contain rounded-xl shadow-2xl transition-transform duration-200"
            :style="{
              transform: `scale(${zoomScale}) rotate(${rotation}deg)`,
            }"
          />
        </div>

        <!-- Modal Bottom Bar -->
        <div class="flex items-center justify-end px-6 py-3 border-t border-slate-200 bg-white">
          <button
            type="button"
            class="px-4 py-2 rounded-xl border border-slate-300 bg-white hover:bg-slate-100 text-slate-700 text-xs font-bold cursor-pointer"
            @click="closeSlipLightbox"
          >
            ปิดหน้าต่าง
          </button>
        </div>
      </div>
    </div>
  </main>
</template>
