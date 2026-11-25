<script setup lang="ts">
import { formatCurrency, formatDate } from '@/shared/utils/formatDate'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import http from '@/shared/api/http'
import { useSwal } from '@/plugins/sweetalert'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { imageUrl } from '@/shared/utils/imageUrl'
import type { AdminPendingSlipGroup } from '../interface/types'

const groups = ref<AdminPendingSlipGroup[]>([])
const loading = ref(true)
const verifyingIndex = ref<number | null>(null)
const swal = useSwal()

// Filtering & Sorting
const searchQuery = ref('')
const sortBy = ref<'newest' | 'oldest' | 'amount_desc' | 'amount_asc'>('newest')

const sortOptions: { value: typeof sortBy.value; label: string; icon: string }[] = [
  { value: 'newest', label: 'ล่าสุดก่อน', icon: 'mdi-sort-clock-descending-outline' },
  { value: 'oldest', label: 'เก่าสุดก่อน', icon: 'mdi-sort-clock-ascending-outline' },
  { value: 'amount_desc', label: 'ยอดสูงสุด', icon: 'mdi-sort-numeric-descending' },
  { value: 'amount_asc', label: 'ยอดต่ำสุด', icon: 'mdi-sort-numeric-ascending' },
]

// Modal Lightbox State
const activeModal = ref<{ group: AdminPendingSlipGroup; index: number } | null>(null)
const zoomScale = ref(1)
const rotation = ref(0)

// Quick Copy Feedback
const copiedId = ref<string | null>(null)

async function loadPendingSlips() {
  loading.value = true
  try {
    const { data } = await http.get<AdminPendingSlipGroup[]>('/orders/admin/pending-slips')
    groups.value = data
  } catch (error) {
    await swal.error('โหลดสลิปที่รออนุมัติไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}

// KPI Stats
const totalPendingAmount = computed(() => {
  return groups.value.reduce((acc, g) => acc + (g.totalGroupAmount || 0), 0)
})

const totalPendingOrders = computed(() => {
  return groups.value.reduce((acc, g) => acc + (g.orders?.length || 0), 0)
})

const uniqueShopsCount = computed(() => {
  const shopSet = new Set<string>()
  for (const g of groups.value) {
    for (const o of g.orders || []) {
      if (o.shopName) shopSet.add(o.shopName)
      else if (o.shopId) shopSet.add(o.shopId)
    }
  }
  return shopSet.size
})

// Filtered & Sorted Groups
const filteredGroups = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()
  let list = groups.value.map((group, index) => ({ group, originalIndex: index }))

  if (q) {
    list = list.filter(({ group }) => {
      const matchBuyer = (group.buyerName || '').toLowerCase().includes(q)
      const matchPhone = (group.buyerPhone || '').includes(q)
      const matchOrder = group.orders?.some(
        (o) =>
          o.orderNumber.toLowerCase().includes(q) ||
          (o.shopName || '').toLowerCase().includes(q) ||
          o.items?.some((it) => it.productName.toLowerCase().includes(q)),
      )
      return matchBuyer || matchPhone || matchOrder
    })
  }

  // Sorting
  list.sort((a, b) => {
    if (sortBy.value === 'newest') {
      const timeA = a.group.slipUploadedAt ? new Date(a.group.slipUploadedAt).getTime() : 0
      const timeB = b.group.slipUploadedAt ? new Date(b.group.slipUploadedAt).getTime() : 0
      return timeB - timeA
    }
    if (sortBy.value === 'oldest') {
      const timeA = a.group.slipUploadedAt ? new Date(a.group.slipUploadedAt).getTime() : 0
      const timeB = b.group.slipUploadedAt ? new Date(b.group.slipUploadedAt).getTime() : 0
      return timeA - timeB
    }
    if (sortBy.value === 'amount_desc') {
      return (b.group.totalGroupAmount || 0) - (a.group.totalGroupAmount || 0)
    }
    if (sortBy.value === 'amount_asc') {
      return (a.group.totalGroupAmount || 0) - (b.group.totalGroupAmount || 0)
    }
    return 0
  })

  return list
})

async function handleVerify(
  group: AdminPendingSlipGroup,
  index: number,
  action: 'Approve' | 'Reject',
) {
  const orderIds = group.orders.map((o) => o.orderId)
  const actionText = action === 'Approve' ? 'อนุมัติสลิปการชำระเงิน' : 'ปฏิเสธสลิปการชำระเงิน'

  const confirm = await swal.confirm(
    `ยืนยัน${actionText}?`,
    action === 'Approve'
      ? `ระบบจะเปลี่ยนสถานะชำระเงินของ ${orderIds.length} ออเดอร์ในกลุ่มนี้เป็น "ชำระเงินแล้ว (Paid)" และส่งแจ้งเตือนให้ร้านค้าเตรียมจัดส่งสินค้าทันที`
      : 'ระบบจะเปลี่ยนสถานะชำระเงินเป็น "ไม่ผ่านการอนุมัติ (Failed)" และแจ้งให้ลูกค้าทราบ',
  )

  if (!confirm.isConfirmed) return

  verifyingIndex.value = index
  try {
    await http.post('/orders/admin/verify-slip', {
      orderIds,
      action,
    })
    await swal.success(
      action === 'Approve' ? 'อนุมัติชำระเงินเรียบร้อย!' : 'ปฏิเสธสลิปเรียบร้อย',
    )
    if (activeModal.value) {
      closeModal()
    }
    await loadPendingSlips()
  } catch (error) {
    await swal.error('ทำรายการไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    verifyingIndex.value = null
  }
}

// Lightbox modal helpers
function openModal(group: AdminPendingSlipGroup, index: number) {
  activeModal.value = { group, index }
  zoomScale.value = 1
  rotation.value = 0
}

function closeModal() {
  activeModal.value = null
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

function openOriginalSlip(url: string) {
  window.open(imageUrl(url), '_blank')
}

async function copyToClipboard(text: string, id: string) {
  try {
    await navigator.clipboard.writeText(text)
    copiedId.value = id
    setTimeout(() => {
      if (copiedId.value === id) copiedId.value = null
    }, 2000)
  } catch {
    // fallback if clipboard access restricted
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && activeModal.value) {
    closeModal()
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
  loadPendingSlips()
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <div class="mx-auto w-full max-w-[1920px] space-y-6 px-4 py-6 text-slate-800 sm:px-6 lg:px-10 lg:py-8">
    <!-- HEADER BANNER -->
    <section
      class="relative overflow-hidden rounded-3xl bg-gradient-to-br from-slate-900 via-slate-800 to-teal-900 p-6 text-white shadow-lg sm:p-8"
    >
      <div class="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full bg-teal-400/20 blur-3xl" />
      <div class="pointer-events-none absolute -bottom-24 left-1/3 h-64 w-64 rounded-full bg-cyan-400/10 blur-3xl" />

      <div class="relative z-10 flex flex-col gap-6 md:flex-row md:items-center md:justify-between">
        <div class="flex items-center gap-4">
          <div
            class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl border border-white/20 bg-white/10 text-3xl text-teal-300 backdrop-blur-md"
          >
            <i class="mdi mdi-receipt-text-check-outline" />
          </div>
          <div>
            <h1 class="text-2xl font-black tracking-tight sm:text-3xl">ตรวจสอบสลิปโอนเงิน</h1>
            <p class="mt-1 text-sm font-bold text-slate-300">
              เทียบยอดเงินกับสลิป แล้วกดอนุมัติเพื่อแจ้งร้านค้าให้จัดส่งสินค้า
            </p>
          </div>
        </div>

        <button
          type="button"
          class="inline-flex cursor-pointer items-center gap-2 self-start rounded-2xl border border-white/25 bg-white/10 px-4 py-2.5 text-sm font-bold text-white backdrop-blur-md transition hover:bg-white/20 active:scale-95 md:self-auto"
          :disabled="loading"
          @click="loadPendingSlips"
        >
          <i class="mdi mdi-refresh text-lg" :class="{ 'animate-spin': loading }" />
          <span>รีเฟรชรายการ</span>
        </button>
      </div>
    </section>

    <!-- KPI CARDS -->
    <section class="grid grid-cols-2 gap-4 lg:grid-cols-4">
      <div class="relative overflow-hidden rounded-2xl border border-slate-300 bg-white p-4 shadow-xs">
        <span class="absolute inset-y-0 left-0 w-1.5 bg-amber-500" />
        <div class="flex items-start justify-between gap-2 pl-1.5">
          <div>
            <p class="text-xs font-black tracking-wide text-slate-600">สลิปรอตรวจสอบ</p>
            <p class="mt-1.5 text-3xl font-black leading-none text-amber-700">{{ groups.length }}</p>
          </div>
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-100 text-xl text-amber-700">
            <i class="mdi mdi-clock-alert-outline" />
          </div>
        </div>
        <p class="mt-3 pl-1.5 text-xs font-bold text-slate-500">รอแอดมินอนุมัติ</p>
      </div>

      <div class="relative overflow-hidden rounded-2xl border border-slate-300 bg-white p-4 shadow-xs">
        <span class="absolute inset-y-0 left-0 w-1.5 bg-teal-600" />
        <div class="flex items-start justify-between gap-2 pl-1.5">
          <div class="min-w-0">
            <p class="text-xs font-black tracking-wide text-slate-600">ยอดเงินรออนุมัติ</p>
            <p class="mt-1.5 truncate text-2xl font-black leading-none text-teal-800 sm:text-3xl">
              {{ formatCurrency(totalPendingAmount) }}
            </p>
          </div>
          <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-teal-100 text-xl text-teal-700">
            <i class="mdi mdi-cash-multiple" />
          </div>
        </div>
        <p class="mt-3 pl-1.5 text-xs font-bold text-slate-500">รวมทุกสลิปในคิว</p>
      </div>

      <div class="relative overflow-hidden rounded-2xl border border-slate-300 bg-white p-4 shadow-xs">
        <span class="absolute inset-y-0 left-0 w-1.5 bg-sky-600" />
        <div class="flex items-start justify-between gap-2 pl-1.5">
          <div>
            <p class="text-xs font-black tracking-wide text-slate-600">คำสั่งซื้อในคิว</p>
            <p class="mt-1.5 text-3xl font-black leading-none text-slate-900">{{ totalPendingOrders }}</p>
          </div>
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-sky-100 text-xl text-sky-700">
            <i class="mdi mdi-package-variant-closed" />
          </div>
        </div>
        <p class="mt-3 pl-1.5 text-xs font-bold text-slate-500">ออเดอร์ที่รอยืนยัน</p>
      </div>

      <div class="relative overflow-hidden rounded-2xl border border-slate-300 bg-white p-4 shadow-xs">
        <span class="absolute inset-y-0 left-0 w-1.5 bg-emerald-600" />
        <div class="flex items-start justify-between gap-2 pl-1.5">
          <div>
            <p class="text-xs font-black tracking-wide text-slate-600">ร้านค้าที่เกี่ยวข้อง</p>
            <p class="mt-1.5 text-3xl font-black leading-none text-slate-900">{{ uniqueShopsCount }}</p>
          </div>
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-100 text-xl text-emerald-700">
            <i class="mdi mdi-store-outline" />
          </div>
        </div>
        <p class="mt-3 pl-1.5 text-xs font-bold text-slate-500">ร้านที่จะได้รับแจ้งจัดส่ง</p>
      </div>
    </section>

    <!-- SEARCH & SORT -->
    <section class="rounded-2xl border border-slate-300 bg-white p-3 shadow-xs sm:p-4">
      <div class="flex flex-col gap-3 xl:flex-row xl:items-center xl:justify-between">
        <div class="relative w-full xl:max-w-xl">
          <i class="mdi mdi-magnify absolute left-4 top-1/2 -translate-y-1/2 text-xl text-slate-500" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="ค้นหาชื่อลูกค้า เบอร์โทร เลขออเดอร์ หรือชื่อร้านค้า..."
            class="w-full rounded-2xl border border-slate-300 bg-slate-50 py-2.5 pl-11 pr-10 text-sm font-bold text-slate-900 placeholder:font-bold placeholder:text-slate-500 focus:border-teal-600 focus:bg-white focus:outline-none focus:ring-4 focus:ring-teal-600/15"
          />
          <button
            v-if="searchQuery"
            type="button"
            class="absolute right-3 top-1/2 -translate-y-1/2 cursor-pointer text-slate-500 hover:text-slate-800"
            title="ล้างคำค้นหา"
            @click="searchQuery = ''"
          >
            <i class="mdi mdi-close-circle text-lg" />
          </button>
        </div>

        <div class="flex flex-wrap items-center gap-3">
          <span class="text-sm font-bold text-slate-600">
            แสดง {{ filteredGroups.length }} จาก {{ groups.length }} สลิป
          </span>
          <div class="flex gap-1.5 overflow-x-auto rounded-2xl bg-slate-100 p-1.5">
            <button
              v-for="opt in sortOptions"
              :key="opt.value"
              type="button"
              class="inline-flex shrink-0 cursor-pointer items-center gap-1.5 rounded-xl px-3 py-2 text-xs font-bold transition sm:text-sm"
              :class="
                sortBy === opt.value
                  ? 'bg-white text-teal-800 shadow-sm ring-1 ring-slate-300'
                  : 'text-slate-600 hover:bg-white/60 hover:text-slate-900'
              "
              @click="sortBy = opt.value"
            >
              <i :class="['mdi text-base', opt.icon]" />
              <span>{{ opt.label }}</span>
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- SKELETON LOADING -->
    <div v-if="loading" class="space-y-4">
      <div
        v-for="i in 3"
        :key="i"
        class="h-64 animate-pulse rounded-2xl border border-slate-300 bg-white shadow-xs"
      ></div>
    </div>

    <!-- EMPTY: ไม่มีสลิปค้าง -->
    <div
      v-else-if="groups.length === 0"
      class="space-y-4 rounded-3xl border border-slate-300 bg-white p-14 text-center shadow-xs"
    >
      <div
        class="mx-auto flex h-20 w-20 items-center justify-center rounded-3xl border border-emerald-200 bg-emerald-100 text-4xl text-emerald-700"
      >
        <i class="mdi mdi-check-decagram" />
      </div>
      <div class="mx-auto max-w-md space-y-1.5">
        <h3 class="text-xl font-black text-slate-900">ตรวจครบแล้ว ไม่มีสลิปค้าง</h3>
        <p class="text-sm font-bold text-slate-600">คำสั่งซื้อทั้งหมดได้รับการตรวจสอบเรียบร้อยแล้ว</p>
      </div>
      <button
        type="button"
        class="inline-flex cursor-pointer items-center gap-2 rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-bold text-slate-700 transition hover:border-teal-600 hover:bg-teal-50 hover:text-teal-800"
        @click="loadPendingSlips"
      >
        <i class="mdi mdi-refresh" /> ตรวจสอบอีกครั้ง
      </button>
    </div>

    <!-- EMPTY: ค้นหาไม่เจอ -->
    <div
      v-else-if="filteredGroups.length === 0"
      class="space-y-3 rounded-3xl border border-slate-300 bg-white p-12 text-center shadow-xs"
    >
      <div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-slate-100 text-2xl text-slate-500">
        <i class="mdi mdi-file-search-outline" />
      </div>
      <h3 class="text-base font-black text-slate-900">ไม่พบสลิปที่ตรงกับ "{{ searchQuery }}"</h3>
      <p class="text-sm font-bold text-slate-600">ลองเปลี่ยนคำค้นหา หรือกดล้างการค้นหา</p>
      <button
        type="button"
        class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl px-3 py-2 text-sm font-bold text-teal-800 underline underline-offset-2 hover:bg-teal-50"
        @click="searchQuery = ''"
      >
        <i class="mdi mdi-backspace-outline" /> ล้างการค้นหา
      </button>
    </div>

    <!-- SLIP CARDS -->
    <div v-else class="space-y-6">
      <article
        v-for="item in filteredGroups"
        :key="item.originalIndex"
        class="overflow-hidden rounded-3xl border border-slate-300 bg-white shadow-xs transition hover:shadow-md"
      >
        <!-- Card header: ผู้โอน + ยอด + ปุ่ม -->
        <div
          class="flex flex-col gap-4 border-b border-slate-300 bg-gradient-to-r from-teal-50 via-white to-white p-5 sm:p-6 lg:flex-row lg:items-center lg:justify-between"
        >
          <div class="flex items-center gap-4">
            <div
              class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-teal-700 text-2xl text-white shadow-xs"
            >
              <i class="mdi mdi-account-cash-outline" />
            </div>
            <div class="min-w-0">
              <div class="flex flex-wrap items-center gap-2">
                <h2 class="text-lg font-black text-slate-900">
                  {{ item.group.buyerName || 'ลูกค้าทั่วไป' }}
                </h2>
                <span
                  class="inline-flex items-center gap-1 rounded-full border border-amber-300 bg-amber-100 px-2.5 py-0.5 text-xs font-black text-amber-800"
                >
                  <i class="mdi mdi-clock-outline" /> รอตรวจสอบ
                </span>
              </div>

              <div class="mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1.5 text-xs font-bold text-slate-600">
                <button
                  v-if="item.group.buyerPhone"
                  type="button"
                  class="inline-flex cursor-pointer items-center gap-1.5 rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-slate-700 transition hover:border-teal-600 hover:text-teal-800 active:scale-95"
                  title="คัดลอกเบอร์โทร"
                  @click="copyToClipboard(item.group.buyerPhone, `phone-${item.originalIndex}`)"
                >
                  <i class="mdi mdi-phone-outline text-sm" />
                  <span>{{ item.group.buyerPhone }}</span>
                  <i
                    class="mdi text-xs"
                    :class="copiedId === `phone-${item.originalIndex}` ? 'mdi-check text-emerald-600' : 'mdi-content-copy text-slate-500'"
                  />
                </button>
                <span class="inline-flex items-center gap-1">
                  <i class="mdi mdi-calendar-clock text-sm text-slate-500" />
                  อัปโหลด
                  {{ formatDate(item.group.slipUploadedAt, { day: 'numeric', month: 'short', year: '2-digit', hour: '2-digit', minute: '2-digit' }) }}
                </span>
                <span class="inline-flex items-center gap-1">
                  <i class="mdi mdi-package-variant-closed text-sm text-slate-500" />
                  {{ item.group.orders?.length || 0 }} คำสั่งซื้อ
                </span>
              </div>
            </div>
          </div>

          <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:gap-5">
            <div class="sm:text-right">
              <p class="text-xs font-black text-slate-600">ยอดที่ต้องโอน</p>
              <p class="text-3xl font-black leading-tight text-teal-800">
                {{ formatCurrency(item.group.totalGroupAmount) }}
              </p>
            </div>

            <div class="flex items-center gap-2">
              <button
                type="button"
                class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-rose-300 bg-rose-50 px-4 py-3 text-sm font-bold text-rose-800 transition hover:bg-rose-600 hover:text-white active:scale-95 disabled:opacity-50"
                :disabled="verifyingIndex === item.originalIndex"
                @click="handleVerify(item.group, item.originalIndex, 'Reject')"
              >
                <i class="mdi mdi-close-circle-outline text-lg" />
                <span>ปฏิเสธ</span>
              </button>

              <button
                type="button"
                class="inline-flex flex-1 cursor-pointer items-center justify-center gap-2 rounded-xl bg-emerald-700 px-6 py-3 text-sm font-black text-white shadow-md transition hover:bg-emerald-800 active:scale-95 disabled:opacity-50 sm:flex-none"
                :disabled="verifyingIndex === item.originalIndex"
                @click="handleVerify(item.group, item.originalIndex, 'Approve')"
              >
                <i
                  class="mdi text-lg"
                  :class="verifyingIndex === item.originalIndex ? 'mdi-loading animate-spin' : 'mdi-check-circle-outline'"
                />
                <span>{{ verifyingIndex === item.originalIndex ? 'กำลังบันทึก...' : 'อนุมัติการชำระเงิน' }}</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Card body -->
        <div class="grid grid-cols-1 items-start gap-6 p-5 sm:p-6 lg:grid-cols-12">
          <!-- สลิป -->
          <div class="space-y-3 rounded-2xl border border-slate-300 bg-slate-50 p-4 lg:col-span-4">
            <p class="flex items-center gap-1.5 text-sm font-black text-slate-800">
              <i class="mdi mdi-file-image-outline text-lg text-teal-700" /> หลักฐานการโอนเงิน
            </p>

            <div
              v-if="item.group.slipImageUrl"
              class="group relative mx-auto aspect-3/4 max-h-80 cursor-pointer overflow-hidden rounded-xl border border-slate-300 bg-white shadow-xs"
              @click="openModal(item.group, item.originalIndex)"
            >
              <img
                :src="imageUrl(item.group.slipImageUrl)"
                alt="สลิปโอนเงิน"
                class="h-full w-full object-contain transition duration-300 group-hover:scale-105"
              />
              <div
                class="absolute inset-0 flex flex-col items-center justify-center gap-1.5 bg-slate-900/60 text-sm font-bold text-white opacity-0 backdrop-blur-xs transition duration-200 group-hover:opacity-100"
              >
                <i class="mdi mdi-magnify-plus-outline text-3xl" />
                <span>แตะเพื่อขยายดู</span>
              </div>
            </div>

            <div
              v-else
              class="rounded-xl border border-dashed border-slate-400 py-12 text-center text-sm font-bold text-slate-500"
            >
              <i class="mdi mdi-image-off-outline mb-1 block text-3xl" />
              ไม่มีรูปสลิปแนบมา
            </div>

            <div class="flex items-center justify-between gap-2 text-sm">
              <button
                type="button"
                class="inline-flex cursor-pointer items-center gap-1.5 rounded-lg px-2.5 py-1.5 font-bold text-teal-800 transition hover:bg-teal-100"
                @click="openModal(item.group, item.originalIndex)"
              >
                <i class="mdi mdi-fullscreen" /> ดูภาพขยาย
              </button>
              <button
                v-if="item.group.slipImageUrl"
                type="button"
                class="inline-flex cursor-pointer items-center gap-1.5 rounded-lg px-2.5 py-1.5 font-bold text-slate-700 transition hover:bg-slate-200"
                @click="openOriginalSlip(item.group.slipImageUrl)"
              >
                <i class="mdi mdi-open-in-new" /> เปิดแท็บใหม่
              </button>
            </div>

            <div class="rounded-xl border border-teal-200 bg-teal-50 p-3 text-xs font-bold text-teal-900">
              <p class="mb-1.5 flex items-center gap-1.5 font-black">
                <i class="mdi mdi-clipboard-check-outline text-base" /> เช็กก่อนอนุมัติ
              </p>
              <ul class="space-y-1">
                <li class="flex items-start gap-1.5">
                  <i class="mdi mdi-check-circle-outline mt-px text-sm" />
                  <span>ยอดในสลิปต้องตรงกับ <strong>{{ formatCurrency(item.group.totalGroupAmount) }}</strong></span>
                </li>
                <li class="flex items-start gap-1.5">
                  <i class="mdi mdi-check-circle-outline mt-px text-sm" />
                  <span>เวลาโอนใกล้เคียงกับเวลาสั่งซื้อ</span>
                </li>
              </ul>
            </div>
          </div>

          <!-- รายการคำสั่งซื้อ -->
          <div class="space-y-4 lg:col-span-8">
            <div class="flex flex-wrap items-center justify-between gap-2">
              <h3 class="flex items-center gap-1.5 text-sm font-black text-slate-800">
                <i class="mdi mdi-store-outline text-lg text-teal-700" />
                <span>คำสั่งซื้อในสลิปนี้ ({{ item.group.orders?.length || 0 }} ร้านค้า)</span>
              </h3>
              <span class="rounded-full bg-slate-100 px-3 py-1 text-xs font-black text-slate-700">
                สินค้ารวม {{ item.group.orders?.reduce((sum, o) => sum + (o.items?.length || 0), 0) }} รายการ
              </span>
            </div>

            <div class="space-y-3.5">
              <div
                v-for="ord in item.group.orders"
                :key="ord.orderId"
                class="overflow-hidden rounded-2xl border border-slate-300 bg-white shadow-2xs"
              >
                <div
                  class="flex flex-wrap items-center justify-between gap-3 border-b border-slate-300 bg-slate-50 p-3.5 text-xs"
                >
                  <div class="flex flex-wrap items-center gap-2">
                    <span
                      class="inline-flex items-center gap-1.5 rounded-xl border border-slate-300 bg-white px-3 py-1.5 text-sm font-black text-slate-900"
                    >
                      <i class="mdi mdi-store text-teal-700" />
                      {{ ord.shopName || 'ร้านค้าชุมชน' }}
                    </span>
                    <button
                      type="button"
                      class="inline-flex cursor-pointer items-center gap-1.5 rounded-lg border border-slate-300 bg-white px-2.5 py-1.5 font-mono text-xs font-bold text-slate-700 transition hover:border-teal-600 hover:text-teal-800 active:scale-95"
                      title="คัดลอกเลขออเดอร์"
                      @click="copyToClipboard(ord.orderNumber, ord.orderId)"
                    >
                      <span>#{{ ord.orderNumber }}</span>
                      <i
                        class="mdi text-xs"
                        :class="copiedId === ord.orderId ? 'mdi-check text-emerald-600' : 'mdi-content-copy text-slate-500'"
                      />
                    </button>
                  </div>

                  <div class="flex items-center gap-3">
                    <span
                      v-if="ord.shippingMethod"
                      class="inline-flex items-center gap-1 rounded-full bg-slate-200 px-2.5 py-1 text-xs font-bold text-slate-700"
                    >
                      <i class="mdi mdi-truck-fast-outline" /> {{ ord.shippingMethod }}
                    </span>
                    <span class="text-base font-black text-slate-900">
                      {{ formatCurrency(ord.totalAmount) }}
                    </span>
                  </div>
                </div>

                <div class="space-y-1 p-3.5">
                  <div
                    v-for="prod in ord.items"
                    :key="prod.orderItemId"
                    class="flex items-center justify-between gap-3 border-b border-slate-200 py-2 text-sm last:border-b-0"
                  >
                    <div class="flex min-w-0 items-center gap-2">
                      <span class="h-2 w-2 shrink-0 rounded-full bg-teal-600" />
                      <span class="truncate font-bold text-slate-800">{{ prod.productName }}</span>
                      <span class="shrink-0 rounded-md bg-slate-100 px-1.5 py-0.5 text-xs font-black text-slate-700">
                        ×{{ prod.quantity }}
                      </span>
                    </div>
                    <span class="shrink-0 font-black text-slate-900">{{ formatCurrency(prod.totalPrice) }}</span>
                  </div>

                  <div
                    v-if="ord.shippingFee !== undefined && ord.shippingFee > 0"
                    class="flex items-center justify-between border-t border-slate-300 pt-2 text-xs font-bold text-slate-600"
                  >
                    <span>ค่าจัดส่งของร้านนี้</span>
                    <span class="font-black text-slate-800">+{{ formatCurrency(ord.shippingFee) }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </article>
    </div>

    <!-- LIGHTBOX: ขยายดูสลิป -->
    <div
      v-if="activeModal"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/85 p-3 backdrop-blur-sm sm:p-6"
      @click="closeModal"
    >
      <div
        class="relative flex max-h-[95vh] w-full max-w-4xl flex-col overflow-hidden rounded-3xl border border-slate-300 bg-white shadow-2xl"
        @click.stop
      >
        <div class="flex items-center justify-between gap-3 bg-gradient-to-br from-slate-900 via-slate-800 to-teal-900 px-5 py-4 text-white">
          <div class="min-w-0">
            <p class="truncate text-sm font-black sm:text-base">
              สลิปโอนเงิน: {{ activeModal.group.buyerName || 'ลูกค้าทั่วไป' }}
            </p>
            <p class="mt-0.5 text-xs font-bold text-slate-300">
              โอนเมื่อ
              {{ formatDate(activeModal.group.slipUploadedAt, { day: 'numeric', month: 'short', year: '2-digit', hour: '2-digit', minute: '2-digit' }) }}
              · ยอด
              <span class="font-black text-teal-300">{{ formatCurrency(activeModal.group.totalGroupAmount) }}</span>
            </p>
          </div>

          <div class="flex shrink-0 items-center gap-1.5">
            <button
              type="button"
              class="flex h-9 w-9 cursor-pointer items-center justify-center rounded-xl bg-white/10 text-lg text-white transition hover:bg-white/25"
              title="ย่อขนาด"
              @click="zoomOut"
            >
              <i class="mdi mdi-magnify-minus-outline" />
            </button>
            <button
              type="button"
              class="flex h-9 w-9 cursor-pointer items-center justify-center rounded-xl bg-white/10 text-lg text-white transition hover:bg-white/25"
              title="ขยายขนาด"
              @click="zoomIn"
            >
              <i class="mdi mdi-magnify-plus-outline" />
            </button>
            <button
              type="button"
              class="flex h-9 w-9 cursor-pointer items-center justify-center rounded-xl bg-white/10 text-lg text-white transition hover:bg-white/25"
              title="หมุนรูป 90 องศา"
              @click="rotateSlip"
            >
              <i class="mdi mdi-rotate-right" />
            </button>
            <button
              type="button"
              class="flex h-9 w-12 cursor-pointer items-center justify-center rounded-xl bg-white/10 text-xs font-black text-white transition hover:bg-white/25"
              title="รีเซ็ตขนาด"
              @click="resetZoom"
            >
              100%
            </button>
            <button
              type="button"
              class="ml-1 flex h-9 w-9 cursor-pointer items-center justify-center rounded-xl bg-rose-500 text-lg text-white transition hover:bg-rose-600"
              title="ปิด (Esc)"
              @click="closeModal"
            >
              <i class="mdi mdi-close" />
            </button>
          </div>
        </div>

        <div class="flex min-h-[400px] flex-1 items-center justify-center overflow-auto bg-slate-900 p-4 sm:p-6">
          <img
            :src="imageUrl(activeModal.group.slipImageUrl)"
            alt="สลิปโอนเงินแบบขยาย"
            class="mx-auto max-h-[65vh] w-auto rounded-xl object-contain shadow-2xl transition-transform duration-200"
            :style="{ transform: `scale(${zoomScale}) rotate(${rotation}deg)` }"
          />
        </div>

        <div
          class="flex flex-wrap items-center justify-between gap-3 border-t border-slate-300 bg-white px-5 py-4"
        >
          <p class="flex items-center gap-2 text-sm font-bold text-slate-600">
            <i class="mdi mdi-information-outline text-lg text-teal-700" />
            <span>ตรวจสลิปแล้วกดอนุมัติได้จากหน้านี้เลย</span>
          </p>

          <div class="flex items-center gap-2">
            <button
              type="button"
              class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-rose-300 bg-rose-50 px-4 py-2.5 text-sm font-bold text-rose-800 transition hover:bg-rose-600 hover:text-white disabled:opacity-50"
              :disabled="verifyingIndex === activeModal.index"
              @click="handleVerify(activeModal.group, activeModal.index, 'Reject')"
            >
              <i class="mdi mdi-close-circle-outline text-lg" /> ปฏิเสธ
            </button>

            <button
              type="button"
              class="inline-flex cursor-pointer items-center gap-2 rounded-xl bg-emerald-700 px-5 py-2.5 text-sm font-black text-white shadow-md transition hover:bg-emerald-800 active:scale-95 disabled:opacity-50"
              :disabled="verifyingIndex === activeModal.index"
              @click="handleVerify(activeModal.group, activeModal.index, 'Approve')"
            >
              <i
                class="mdi text-lg"
                :class="verifyingIndex === activeModal.index ? 'mdi-loading animate-spin' : 'mdi-check-circle-outline'"
              />
              <span>{{ verifyingIndex === activeModal.index ? 'กำลังบันทึก...' : 'อนุมัติการชำระเงิน' }}</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
