<script setup lang="ts">
// Public storefront - Orange + Cream Order Detail View
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import http from '@/shared/api/http'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'

import OrderDetailHeader from '../components/order-detail/OrderDetailHeader.vue'
import OrderProgressTimeline from '../components/order-detail/OrderProgressTimeline.vue'
import OrderItemsList, { type OrderItem } from '../components/order-detail/OrderItemsList.vue'
import OrderShippingInfo, { type Shipment } from '../components/order-detail/OrderShippingInfo.vue'
import OrderCostSummary from '../components/order-detail/OrderCostSummary.vue'

import { getMyReviewedProducts } from '@/features/shops/api/productApi'

interface Order {
  orderId: string
  orderNumber: string
  subtotal: number
  shippingFee: number
  totalAmount: number
  orderStatus: string
  paymentStatus: string
  paymentExpiresAt?: string | null
  createdAt: string
  items: OrderItem[]
  shippingMethod: string
  receiverName?: string
  receiverPhone?: string
  shippingAddress?: string
  shipment?: Shipment
}

const route = useRoute()
const order = ref<Order | null>(null)
const reviewedProductIds = ref<string[]>([])
const loading = ref(true)
const confirmingReceipt = ref(false)
const swal = useSwal()
const now = ref(Date.now())
let paymentTimer: ReturnType<typeof setInterval> | undefined
function toUtcMilliseconds(value: string) {
  const isoValue = value.replace(' ', 'T')
  const hasTimezone = /(?:Z|[+-]\d{2}:\d{2})$/i.test(isoValue)
  return new Date(hasTimezone ? isoValue : `${isoValue}Z`).getTime()
}
const paymentExpiresAt = computed(() => {
  if (!order.value) return null
  return order.value.paymentExpiresAt ?? new Date(toUtcMilliseconds(order.value.createdAt) + 30 * 60 * 1000).toISOString()
})
const paymentExpiresAtMs = computed(() => (paymentExpiresAt.value ? toUtcMilliseconds(paymentExpiresAt.value) : null))
const isAwaitingPayment = computed(() => order.value?.orderStatus === 'Pending' && order.value?.paymentStatus === 'Pending')
const isExpired = computed(() => order.value?.orderStatus === 'Expired' || order.value?.paymentStatus === 'Expired' || (isAwaitingPayment.value && !!paymentExpiresAt.value && toUtcMilliseconds(paymentExpiresAt.value) <= now.value))

const totalItems = computed(
  () => order.value?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0,
)

// Progress Steps calculation for Timeline
const currentStepIndex = computed(() => {
  if (!order.value) return 1
  if (order.value.orderStatus === 'Cancelled' || isExpired.value) return 0
  if (order.value.orderStatus === 'Completed') return 4
  if (order.value.orderStatus === 'Shipped') return 3
  if (order.value.orderStatus === 'Confirmed' || order.value.paymentStatus === 'Paid') return 2
  return 1
})

const isPendingVerification = computed(() => order.value?.paymentStatus === 'PendingVerification')
const canPay = computed(
  () =>
    order.value?.paymentStatus !== 'Paid' &&
    !isPendingVerification.value &&
    order.value?.orderStatus !== 'Cancelled' &&
    !isExpired.value,
)
const canConfirmReceipt = computed(
  () => order.value?.paymentStatus === 'Paid' && order.value?.orderStatus === 'Shipped',
)

async function confirmReceipt() {
  if (!order.value) return

  const confirmation = await swal.confirm(
    'ยืนยันว่าได้รับสินค้าแล้ว?',
    'หลังยืนยัน คำสั่งซื้อจะสำเร็จสมบูรณ์และคุณสามารถเขียนรีวิวสินค้าได้',
  )

  if (!confirmation.isConfirmed) return

  confirmingReceipt.value = true
  try {
    await http.patch(`/orders/${order.value.orderId}/shipment/status`, {
      status: 'Completed',
    })
    order.value.orderStatus = 'Completed'
    if (order.value.shipment) {
      order.value.shipment.shippingStatus = 'Delivered'
      order.value.shipment.deliveredAt = new Date().toISOString()
    }
    await swal.success('ยืนยันรับสินค้าแล้ว', 'คุณสามารถเขียนรีวิวสินค้าที่ซื้อได้แล้ว')
  } catch (error) {
    await swal.error('ยืนยันรับสินค้าไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    confirmingReceipt.value = false
  }
}

async function fetchMyReviewedProducts() {
  try {
    reviewedProductIds.value = order.value
      ? await getMyReviewedProducts(order.value.orderId)
      : []
  } catch {
    reviewedProductIds.value = []
  }
}

onMounted(async () => {
  paymentTimer = setInterval(() => { now.value = Date.now() }, 1000)
  try {
    const [{ data }] = await Promise.all([
      http.get<Order>(`/orders/${route.params.id}`),
    ])
    order.value = data
    await fetchMyReviewedProducts()
  } catch (error) {
    await swal.error('ไม่พบออเดอร์', getApiErrorMessage(error, 'คำสั่งซื้อนี้อาจไม่มีอยู่ในระบบ'))
  } finally {
    loading.value = false
  }
})
onBeforeUnmount(() => { if (paymentTimer) clearInterval(paymentTimer) })
</script>

<template>
  <div class="min-h-screen bg-[#F7F0E6] pb-24 text-[#332820]">
    <!-- Top bar -->
    <div class="border-b border-[#E8D9C9] bg-white">
      <div class="mx-auto flex max-w-[1400px] items-center justify-between px-4 py-3 sm:px-6 lg:px-10">
        <RouterLink
          to="/orders"
          class="inline-flex items-center gap-2 rounded-full border border-[#E8D9C9] bg-white px-4 py-2 text-xs font-black text-[#332820] transition hover:border-[#D96C2C] hover:text-[#D96C2C] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none sm:text-sm"
        >
          <i class="mdi mdi-arrow-left text-base text-[#D96C2C]"></i>
          <span>กลับไปรายการคำสั่งซื้อ</span>
        </RouterLink>
      </div>
    </div>

    <main class="mx-auto max-w-[1400px] px-4 py-6 sm:px-6 sm:py-8 lg:px-10">
      <!-- Loading skeleton -->
      <div v-if="loading" class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_24rem]">
        <div class="space-y-6">
          <div class="h-56 animate-pulse rounded-3xl border border-[#E8D9C9] bg-white"></div>
          <div class="h-40 animate-pulse rounded-3xl border border-[#E8D9C9] bg-white"></div>
        </div>
        <div class="h-80 animate-pulse rounded-3xl border border-[#E8D9C9] bg-white"></div>
      </div>

      <div v-else-if="order" class="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_24rem]">
        <!-- LEFT: status, progress, items -->
        <div class="min-w-0 space-y-6">
          <OrderDetailHeader
            :order-number="order.orderNumber"
            :created-at="order.createdAt"
            :payment-status="order.paymentStatus"
            :order-status="order.orderStatus"
            :expired="isExpired"
            :awaiting-payment="isAwaitingPayment"
            :pending-verification="isPendingVerification"
            :expires-at-ms="paymentExpiresAtMs"
            :now="now"
          />

          <OrderProgressTimeline
            v-if="order.orderStatus !== 'Cancelled' && !isExpired"
            :current-step-index="currentStepIndex"
          />

          <OrderItemsList
            :items="order.items"
            :order-id="order.orderId"
            :total-items="totalItems"
            :order-status="order.orderStatus"
            :reviewed-product-ids="reviewedProductIds"
            @review-added="fetchMyReviewedProducts"
          />
        </div>

        <!-- RIGHT: payment summary + delivery (stays in view while scrolling) -->
        <aside class="space-y-6 lg:sticky lg:top-28">
          <OrderCostSummary
            :subtotal="order.subtotal"
            :shipping-fee="order.shippingFee"
            :total-amount="order.totalAmount"
            :order-id="order.orderId"
            :can-pay="canPay"
            :can-confirm-receipt="canConfirmReceipt && !confirmingReceipt"
            @confirm-receipt="confirmReceipt"
          />

          <OrderShippingInfo
            :shipping-method="order.shippingMethod"
            :receiver-name="order.receiverName"
            :receiver-phone="order.receiverPhone"
            :shipping-address="order.shippingAddress"
            :shipment="order.shipment"
          />
        </aside>
      </div>

      <div v-else class="space-y-2 py-20 text-center text-[#786B62]">
        <i class="mdi mdi-text-box-remove-outline mb-2 block text-4xl text-[#D96C2C]"></i>
        <span class="text-base font-black text-[#332820]">ไม่พบข้อมูลคำสั่งซื้อนี้</span>
      </div>
    </main>
  </div>
</template>
