<script setup lang="ts">
import { ref } from 'vue'
import { formatDate } from '@/shared/utils/formatDate'
import ShipmentFulfillmentForm from './ShipmentFulfillmentForm.vue'

export interface OrderItem {
  orderId: string
  orderNumber: string
  totalAmount: number
  orderStatus: string
  paymentStatus: string
  shippingMethod: string
  createdAt: string
  slipImageUrl?: string
  slipUploadedAt?: string
  shippingProvider?: string
  trackingNumber?: string
  items: {
    orderItemId: string
    productName: string
    imageUrl?: string | null
    quantity: number
    unitPrice: number
    totalPrice: number
  }[]
}

const props = defineProps<{
  order: OrderItem
  updatingId?: string | null
}>()

const emit = defineEmits<{
  (e: 'update-status', payload: { order: OrderItem; status: string }): void
  (e: 'ship-order', payload: { order: OrderItem; provider: string; trackingNumber: string }): void
}>()

const copiedOrderNum = ref(false)
const copiedTracking = ref(false)
const showSlipModal = ref(false)
const showFulfillmentForm = ref(false)

function copyToClipboard(text: string, type: 'order' | 'tracking') {
  navigator.clipboard.writeText(text).then(() => {
    if (type === 'order') {
      copiedOrderNum.value = true
      setTimeout(() => (copiedOrderNum.value = false), 2000)
    } else {
      copiedTracking.value = true
      setTimeout(() => (copiedTracking.value = false), 2000)
    }
  })
}

function getStatusTitle(status: string) {
  if (status === 'Confirmed') return 'ยืนยันออเดอร์แล้ว'
  if (status === 'Pending') return 'รอยืนยันออเดอร์'
  if (status === 'Processing') return 'กำลังเตรียมจัดส่ง'
  if (status === 'Shipped') return 'จัดส่งสินค้าแล้ว'
  if (status === 'Completed') return 'คำสั่งซื้อสำเร็จ'
  if (status === 'Cancelled') return 'ยกเลิกออเดอร์แล้ว'
  return status
}

const apiOrigin = (import.meta.env.VITE_API_URL ?? 'https://localhost:7289/api').replace(
  /\/api$/,
  '',
)

function imageUrl(url?: string | null) {
  if (!url) return ''
  return url.startsWith('/') ? `${apiOrigin}${url}` : url
}
</script>

<template>
  <article
    class="rounded-2xl border-2 border-slate-200 bg-white p-5 sm:p-6 shadow-sm transition hover:border-slate-400 space-y-4 text-slate-900"
  >
    <!-- 1. Header: Order #, Date, Status Badges, and Grand Total -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b-2 border-slate-100 pb-4">
      <div class="space-y-1.5">
        <div class="flex flex-wrap items-center gap-2">
          <!-- Order Number with High Contrast Badge -->
          <div class="inline-flex items-center gap-2 bg-slate-900 text-white px-3 py-1 rounded-xl font-mono text-sm font-bold shadow-2xs">
            <span>#{{ order.orderNumber }}</span>
            <button
              type="button"
              class="text-slate-300 hover:text-white transition cursor-pointer"
              :title="copiedOrderNum ? 'คัดลอกแล้ว' : 'คัดลอกเลขออเดอร์'"
              @click="copyToClipboard(order.orderNumber, 'order')"
            >
              <i :class="['mdi text-xs', copiedOrderNum ? 'mdi-check text-emerald-400' : 'mdi-content-copy']"></i>
            </button>
          </div>

          <!-- Payment Status (Solid, Vivid High-Contrast Pill) -->
          <span
            v-if="order.paymentStatus === 'Paid'"
            class="inline-flex items-center gap-1.5 rounded-xl bg-emerald-600 text-white px-3 py-1 text-xs font-bold shadow-2xs"
          >
            <i class="mdi mdi-check-circle text-xs"></i>
            <span>ชำระเงินแล้ว</span>
          </span>
          <span
            v-else-if="order.paymentStatus === 'PendingVerification'"
            class="inline-flex items-center gap-1.5 rounded-xl bg-amber-500 text-white px-3 py-1 text-xs font-bold shadow-2xs"
          >
            <i class="mdi mdi-clock-outline text-xs"></i>
            <span>รอตรวจสลิป</span>
          </span>
          <span
            v-else
            class="inline-flex items-center gap-1.5 rounded-xl bg-rose-600 text-white px-3 py-1 text-xs font-bold shadow-2xs"
          >
            <i class="mdi mdi-alert-circle text-xs"></i>
            <span>รอชำระเงิน</span>
          </span>

          <!-- Order Status (Crisp Border & Bold Dark Text) -->
          <span
            class="inline-flex items-center gap-1 rounded-xl px-3 py-1 text-xs font-bold border-2"
            :class="{
              'bg-amber-100 text-amber-950 border-amber-400': order.orderStatus === 'Pending',
              'bg-blue-100 text-blue-950 border-blue-400': order.orderStatus === 'Confirmed',
              'bg-teal-100 text-teal-950 border-teal-400': order.orderStatus === 'Processing',
              'bg-emerald-100 text-emerald-950 border-emerald-400': order.orderStatus === 'Shipped' || order.orderStatus === 'Completed',
              'bg-slate-200 text-slate-800 border-slate-300': order.orderStatus === 'Cancelled',
            }"
          >
            <span>สถานะ: {{ getStatusTitle(order.orderStatus) }}</span>
          </span>

          <!-- View Slip Button -->
          <button
            v-if="order.slipImageUrl"
            type="button"
            class="inline-flex items-center gap-1.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white px-3 py-1 text-xs font-bold shadow-2xs transition cursor-pointer select-none"
            @click="showSlipModal = true"
          >
            <i class="mdi mdi-receipt-text-outline text-xs"></i>
            <span>ดูสลิปโอนเงิน</span>
          </button>
        </div>

        <div class="text-xs font-bold text-slate-700 flex items-center gap-1.5">
          <i class="mdi mdi-calendar-clock text-slate-500 text-sm"></i>
          <span>สั่งซื้อเมื่อ: {{ formatDate(order.createdAt, { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }) }}</span>
        </div>
      </div>

      <!-- Grand Total Price (High Contrast, Bold, Clear) -->
      <div class="text-left md:text-right bg-slate-50 md:bg-transparent p-3 md:p-0 rounded-xl border md:border-0 border-slate-200">
        <span class="block text-xs font-bold text-slate-600 uppercase tracking-wide">ยอดรวมคำสั่งซื้อ</span>
        <span class="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
          ฿{{ Number(order.totalAmount).toLocaleString('th-TH') }}
        </span>
      </div>
    </div>

    <!-- 2. Product Items in Order (Clean, Clear Contrast, Dark Font) -->
    <div class="space-y-2.5">
      <div
        v-for="item in order.items"
        :key="item.orderItemId"
        class="flex items-center justify-between gap-4 rounded-xl bg-slate-50 p-3 border border-slate-200"
      >
        <div class="flex items-center gap-3.5 min-w-0">
          <div class="h-12 w-12 rounded-xl bg-white border border-slate-300 overflow-hidden shrink-0 flex items-center justify-center shadow-2xs">
            <img
              v-if="item.imageUrl"
              :src="imageUrl(item.imageUrl)"
              :alt="item.productName"
              class="h-full w-full object-cover"
            />
            <i v-else class="mdi mdi-package-variant-closed text-xl text-slate-400"></i>
          </div>
          <div class="min-w-0">
            <p class="text-sm font-bold text-slate-950 truncate">{{ item.productName }}</p>
            <p class="text-xs font-bold text-slate-600 mt-0.5">
              ราคา ฿{{ Number(item.unitPrice).toLocaleString('th-TH') }} × {{ item.quantity }} ชิ้น
            </p>
          </div>
        </div>
        <span class="text-sm font-black text-slate-950 shrink-0">
          ฿{{ Number(item.totalPrice).toLocaleString('th-TH') }}
        </span>
      </div>
    </div>

    <!-- 3. Bottom Bar: Shipping Info & High Contrast Action Buttons -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-2 border-t border-slate-200">
      <!-- Shipping status / info -->
      <div class="flex items-center gap-2 text-xs font-bold text-slate-700">
        <i class="mdi mdi-truck-delivery text-slate-600 text-base"></i>
        <span>การจัดส่ง:</span>
        <span
          v-if="order.orderStatus === 'Shipped' || order.orderStatus === 'Completed'"
          class="inline-flex items-center gap-2 bg-emerald-100 text-emerald-950 border border-emerald-300 px-3 py-1 rounded-lg"
        >
          <span>{{ order.shippingProvider || 'ขนส่งพัสดุ' }}</span>
          <span class="font-mono font-black text-sm text-emerald-900">{{ order.trackingNumber }}</span>
          <button
            v-if="order.trackingNumber"
            type="button"
            class="text-emerald-700 hover:text-emerald-950 cursor-pointer ml-1"
            :title="copiedTracking ? 'คัดลอกแล้ว' : 'คัดลอกเลขพัสดุ'"
            @click="copyToClipboard(order.trackingNumber, 'tracking')"
          >
            <i :class="['mdi text-xs', copiedTracking ? 'mdi-check text-emerald-950' : 'mdi-content-copy']"></i>
          </button>
        </span>
        <span v-else class="text-slate-800 font-semibold">
          จัดส่งทางพัสดุมาตรฐาน
        </span>
      </div>

      <!-- Action Buttons (Bold, High Contrast, Clear) -->
      <div class="flex items-center gap-2.5 self-end sm:self-auto">
        <!-- If Pending -->
        <template v-if="order.orderStatus === 'Pending'">
          <button
            type="button"
            class="rounded-xl border-2 border-rose-300 bg-white hover:bg-rose-50 px-4 py-2 text-xs sm:text-sm font-bold text-rose-700 transition disabled:opacity-50 cursor-pointer"
            :disabled="updatingId === order.orderId"
            @click="emit('update-status', { order, status: 'Cancelled' })"
          >
            ยกเลิกออเดอร์
          </button>
          <button
            type="button"
            class="inline-flex items-center gap-1.5 rounded-xl bg-emerald-700 hover:bg-emerald-800 px-5 py-2 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 disabled:opacity-50 cursor-pointer border border-emerald-700"
            :disabled="updatingId === order.orderId"
            @click="emit('update-status', { order, status: 'Confirmed' })"
          >
            <i class="mdi mdi-check-circle text-base text-white"></i>
            <span>ยืนยันออเดอร์</span>
          </button>
        </template>

        <!-- If Confirmed -->
        <template v-else-if="order.orderStatus === 'Confirmed'">
          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-xl bg-teal-700 hover:bg-teal-800 px-5 py-2 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 disabled:opacity-50 cursor-pointer border border-teal-700"
            :disabled="updatingId === order.orderId"
            @click="emit('update-status', { order, status: 'Processing' })"
          >
            <i class="mdi mdi-package-variant-closed-check text-base text-white"></i>
            <span>เริ่มเตรียมจัดส่งสินค้า</span>
          </button>
        </template>

        <!-- If Processing & Paid -->
        <template v-else-if="order.orderStatus === 'Processing' && order.paymentStatus === 'Paid'">
          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-xl bg-emerald-700 hover:bg-emerald-800 px-5 py-2 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 cursor-pointer border border-emerald-700"
            @click="showFulfillmentForm = !showFulfillmentForm"
          >
            <i class="mdi mdi-truck-fast text-base text-white"></i>
            <span>{{ showFulfillmentForm ? 'ปิดแบบฟอร์ม' : 'กรอกเลขพัสดุจัดส่ง' }}</span>
          </button>
        </template>

        <!-- If Waiting for payment/verification warning -->
        <div
          v-else-if="order.paymentStatus !== 'Paid' && order.orderStatus !== 'Cancelled'"
          class="text-xs font-bold text-amber-950 bg-amber-100 border border-amber-300 px-3 py-1.5 rounded-xl"
        >
          {{ order.paymentStatus === 'PendingVerification' ? 'รอ Admin ตรวจสอบสลิป' : 'รอลูกค้าชำระเงิน' }}
        </div>
      </div>
    </div>

    <!-- Fulfillment Form Accordion -->
    <div v-if="showFulfillmentForm && order.paymentStatus === 'Paid' && order.orderStatus === 'Processing'" class="pt-2">
      <ShipmentFulfillmentForm
        :updating="updatingId === order.orderId"
        @submit="
          (payload) => {
            emit('ship-order', { order, ...payload })
            showFulfillmentForm = false
          }
        "
      />
    </div>

    <!-- Slip Image Modal -->
    <Teleport to="body">
      <dialog
        v-if="showSlipModal && order.slipImageUrl"
        class="fixed inset-0 z-50 m-auto max-h-[90vh] max-w-lg w-full overflow-hidden rounded-3xl border-2 border-slate-300 bg-white p-6 shadow-2xl backdrop:bg-slate-950/70"
        open
      >
        <div class="space-y-4">
          <div class="flex items-center justify-between border-b-2 border-slate-100 pb-3">
            <div class="flex items-center gap-2.5">
              <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-indigo-600 text-white font-bold">
                <i class="mdi mdi-receipt-text-outline text-xl text-white"></i>
              </span>
              <div>
                <h3 class="text-base font-bold text-slate-950">หลักฐานสลิปการโอนเงิน</h3>
                <p class="text-xs font-bold text-slate-500 font-mono">ออเดอร์ #{{ order.orderNumber }}</p>
              </div>
            </div>
            <button
              type="button"
              class="rounded-xl p-1.5 text-slate-500 hover:bg-slate-100 hover:text-slate-900 transition cursor-pointer"
              @click="showSlipModal = false"
            >
              <i class="mdi mdi-close text-2xl"></i>
            </button>
          </div>

          <div class="aspect-3/4 max-h-[60vh] w-full overflow-hidden rounded-2xl border-2 border-slate-200 bg-slate-50 flex items-center justify-center">
            <img
              :src="imageUrl(order.slipImageUrl)"
              alt="สลิปโอนเงิน"
              class="h-full w-full object-contain"
            />
          </div>

          <div class="flex items-center justify-end pt-2">
            <button
              type="button"
              class="rounded-xl bg-slate-900 text-white px-5 py-2 text-xs font-bold hover:bg-slate-800 transition cursor-pointer"
              @click="showSlipModal = false"
            >
              ปิดหน้าต่าง
            </button>
          </div>
        </div>
      </dialog>
    </Teleport>
  </article>
</template>

<style scoped>
button {
  outline: none !important;
  -webkit-tap-highlight-color: transparent !important;
}

button:focus,
button:focus-visible,
button:active {
  outline: none !important;
  box-shadow: none !important;
}
</style>
