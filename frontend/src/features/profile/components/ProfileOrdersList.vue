<script setup lang="ts">
import { formatDate } from '@/shared/utils/formatDate'
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'

export interface OrderItem {
  orderItemId: string
  productId: string
  productName: string
  imageUrl?: string | null
  quantity: number
  unitPrice: number
  TotalPrice: number
}

export interface OrderShipment {
  shippingProvider?: string | null
  trackingNumber?: string | null
  shippingStatus?: string
  shippedAt?: string | null
  deliveredAt?: string | null
}

export interface RichOrder {
  orderId: string
  shopId: string
  shopName?: string | null
  shopLogoUrl?: string | null
  orderNumber: string
  subtotal: number
  shippingFee: number
  totalAmount: number
  shippingMethod: string
  orderStatus: string
  paymentStatus: string
  paymentExpiresAt?: string | null
  createdAt: string
  receiverName?: string | null
  receiverPhone?: string | null
  shippingAddress?: string | null
  items: OrderItem[]
  shipment?: OrderShipment | null
}

interface Props {
  orders: RichOrder[]
  loading?: boolean
}

const props = defineProps<Props>()

const selectedTab = ref<'all' | 'to_pay' | 'to_ship' | 'to_receive' | 'completed' | 'cancelled'>(
  'all',
)
const searchQuery = ref('')
const now = ref(Date.now())
let paymentTimer: ReturnType<typeof setInterval> | undefined

function toUtcMilliseconds(value: string) {
  const isoValue = value.replace(' ', 'T')
  return new Date(/(?:Z|[+-]\d{2}:\d{2})$/i.test(isoValue) ? isoValue : `${isoValue}Z`).getTime()
}

function paymentExpiry(order: RichOrder) {
  return order.paymentExpiresAt
    ? toUtcMilliseconds(order.paymentExpiresAt)
    : toUtcMilliseconds(order.createdAt) + 30 * 60 * 1000
}

function isAwaitingPayment(order: RichOrder) {
  return order.orderStatus === 'Pending' && order.paymentStatus === 'Pending'
}

function isPaymentExpired(order: RichOrder) {
  return (
    order.orderStatus === 'Expired' ||
    order.paymentStatus === 'Expired' ||
    (isAwaitingPayment(order) && paymentExpiry(order) <= now.value)
  )
}

function paymentDeadlineText(order: RichOrder) {
  if (isPaymentExpired(order)) return 'หมดเวลาชำระเงินแล้ว กรุณาสั่งซื้อสินค้าใหม่อีกครั้ง'
  const seconds = Math.ceil((paymentExpiry(order) - now.value) / 1000)
  return `กรุณาชำระภายใน ${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')} นาที`
}

onMounted(() => {
  paymentTimer = setInterval(() => {
    now.value = Date.now()
  }, 1000)
})
onBeforeUnmount(() => {
  if (paymentTimer) clearInterval(paymentTimer)
})

const apiOrigin = (import.meta.env.VITE_API_URL ?? 'https://localhost:7289/api').replace(
  /\/api$/,
  '',
)

function imageUrl(url?: string | null) {
  if (!url) return ''
  return url.startsWith('/') ? `${apiOrigin}${url}` : url
}



// Counts for filter pills
const counts = computed(() => {
  return {
    all: props.orders.length,
    to_pay: props.orders.filter((o) => isAwaitingPayment(o) && !isPaymentExpired(o)).length,
    to_ship: props.orders.filter(
      (o) =>
        o.paymentStatus === 'Paid' &&
        (o.orderStatus === 'Confirmed' || o.orderStatus === 'Pending'),
    ).length,
    to_receive: props.orders.filter((o) => o.orderStatus === 'Shipped').length,
    completed: props.orders.filter((o) => o.orderStatus === 'Completed').length,
    cancelled: props.orders.filter((o) => o.orderStatus === 'Cancelled').length,
  }
})

// Filtered and searched orders
const filteredOrders = computed(() => {
  let list = props.orders

  // Status Filter
  if (selectedTab.value === 'to_pay') {
    list = list.filter((o) => isAwaitingPayment(o) && !isPaymentExpired(o))
  } else if (selectedTab.value === 'to_ship') {
    list = list.filter(
      (o) =>
        o.paymentStatus === 'Paid' &&
        (o.orderStatus === 'Confirmed' || o.orderStatus === 'Pending'),
    )
  } else if (selectedTab.value === 'to_receive') {
    list = list.filter((o) => o.orderStatus === 'Shipped')
  } else if (selectedTab.value === 'completed') {
    list = list.filter((o) => o.orderStatus === 'Completed')
  } else if (selectedTab.value === 'cancelled') {
    list = list.filter((o) => o.orderStatus === 'Cancelled')
  }

  // Search Query (Order number, shop name, product name)
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.trim().toLowerCase()
    list = list.filter(
      (o) =>
        o.orderNumber.toLowerCase().includes(q) ||
        (o.shopName && o.shopName.toLowerCase().includes(q)) ||
        o.items.some((i) => i.productName.toLowerCase().includes(q)),
    )
  }

  return list
})
type OrderTab = 'all' | 'to_pay' | 'to_ship' | 'to_receive' | 'completed' | 'cancelled'

const tabs = computed<{ id: OrderTab; label: string; icon: string; count: number }[]>(() => [
  { id: 'all', label: 'ทั้งหมด', icon: 'mdi-view-list-outline', count: counts.value.all },
  { id: 'to_pay', label: 'ที่ต้องชำระ', icon: 'mdi-credit-card-clock-outline', count: counts.value.to_pay },
  { id: 'to_ship', label: 'ที่ต้องจัดส่ง', icon: 'mdi-package-variant', count: counts.value.to_ship },
  { id: 'to_receive', label: 'ที่ต้องได้รับ', icon: 'mdi-truck-fast-outline', count: counts.value.to_receive },
  { id: 'completed', label: 'สำเร็จแล้ว', icon: 'mdi-check-circle-outline', count: counts.value.completed },
  { id: 'cancelled', label: 'ยกเลิกแล้ว', icon: 'mdi-close-circle-outline', count: counts.value.cancelled },
])

// One clear status chip per order
function statusInfo(order: RichOrder) {
  if (isPaymentExpired(order))
    return { label: 'หมดเวลาชำระเงิน', icon: 'mdi-clock-remove-outline', text: 'text-slate-500' }
  if (order.orderStatus === 'Shipped')
    return { label: 'กำลังจัดส่ง', icon: 'mdi-truck-fast', text: 'text-sky-700' }
  if (order.orderStatus === 'Completed')
    return { label: 'จัดส่งสำเร็จ', icon: 'mdi-check-circle', text: 'text-emerald-700' }
  if (order.orderStatus === 'Cancelled')
    return { label: 'ยกเลิกแล้ว', icon: 'mdi-close-circle', text: 'text-rose-600' }
  if (order.paymentStatus === 'PendingVerification')
    return { label: 'รอแอดมินตรวจสอบสลิป', icon: 'mdi-file-search-outline', text: 'text-violet-700' }
  if (order.paymentStatus === 'Paid')
    return { label: 'ร้านกำลังเตรียมสินค้า', icon: 'mdi-package-variant', text: 'text-[#D96C2C]' }
  if (isAwaitingPayment(order))
    return { label: 'รอชำระเงิน', icon: 'mdi-credit-card-clock-outline', text: 'text-amber-700' }
  return { label: 'รอยืนยันคำสั่งซื้อ', icon: 'mdi-clock-outline', text: 'text-[#D96C2C]' }
}
</script>

<template>
  <div class="space-y-4">
    <!-- Title + shop button -->
    <div class="flex items-center justify-between gap-3">
      <h2 class="text-xl leading-[1.35] font-black text-[#332820] sm:text-2xl">การซื้อของฉัน</h2>
      <RouterLink
        to="/shops"
        class="inline-flex shrink-0 items-center gap-1.5 text-sm font-black text-[#D96C2C] hover:underline focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
      >
        เลือกซื้อสินค้าเพิ่ม <i class="mdi mdi-arrow-right"></i>
      </RouterLink>
    </div>

    <!-- Simple tabs -->
    <div class="rounded-2xl border border-[#E8D9C9] bg-white">
      <div class="flex overflow-x-auto border-b border-[#E8D9C9] px-2" role="tablist" aria-label="สถานะคำสั่งซื้อ">
        <button
          v-for="t in tabs"
          :key="t.id"
          type="button"
          role="tab"
          :aria-selected="selectedTab === t.id"
          class="relative shrink-0 cursor-pointer px-4 py-3.5 text-sm font-black whitespace-nowrap transition focus-visible:outline-none"
          :class="selectedTab === t.id ? 'text-[#D96C2C]' : 'text-[#786B62] hover:text-[#332820]'"
          @click="selectedTab = t.id"
        >
          {{ t.label }}
          <span v-if="t.count > 0" class="ml-1 text-xs font-bold" :class="selectedTab === t.id ? 'text-[#D96C2C]' : 'text-[#A8988C]'">
            {{ t.count }}
          </span>
          <span
            v-if="selectedTab === t.id"
            class="absolute inset-x-3 bottom-0 h-0.5 rounded-full bg-[#D96C2C]"
          ></span>
        </button>
      </div>

      <div class="relative p-3">
        <i class="mdi mdi-magnify absolute top-1/2 left-6 -translate-y-1/2 text-xl text-[#A8988C]"></i>
        <input
          v-model="searchQuery"
          type="text"
          class="w-full rounded-xl bg-[#F7F0E6] py-2.5 pr-4 pl-10 text-sm font-semibold text-[#332820] outline-none transition placeholder:text-[#A8988C] focus:bg-white focus:ring-2 focus:ring-[#D96C2C]/30"
          placeholder="ค้นหาชื่อร้าน เลขคำสั่งซื้อ หรือชื่อสินค้า"
        />
      </div>
    </div>

    <!-- Empty -->
    <div v-if="!filteredOrders.length" class="rounded-2xl border border-[#E8D9C9] bg-white px-6 py-16 text-center">
      <i class="mdi mdi-receipt-text-outline text-5xl text-[#C9B8A8]"></i>
      <p class="mt-3 text-base font-black text-[#332820]">ยังไม่มีคำสั่งซื้อในหมวดนี้</p>
      <RouterLink
        to="/shops"
        class="mt-4 inline-flex items-center gap-2 rounded-full bg-[#D96C2C] px-6 py-2.5 text-sm font-black text-white transition hover:bg-[#BF5720]"
      >
        <span class="text-white">เลือกซื้อสินค้า</span>
      </RouterLink>
    </div>

    <!-- Orders: one calm card each -->
    <div v-else class="space-y-3">
      <article
        v-for="order in filteredOrders"
        :key="order.orderId"
        class="rounded-2xl border border-[#E8D9C9] bg-white"
      >
        <!-- Shop + status -->
        <div class="flex items-center justify-between gap-3 border-b border-[#E8D9C9]/70 px-4 py-3 sm:px-5">
          <div class="min-w-0">
            <RouterLink
              v-if="order.shopId"
              :to="`/shops/${order.shopId}`"
              class="block truncate text-sm font-black text-[#332820] hover:text-[#D96C2C]"
            >
              {{ order.shopName || 'ร้านค้าชุมชนกาญจนบุรี' }}
            </RouterLink>
            <p class="text-xs font-semibold text-[#786B62]">
              #{{ order.orderNumber }} · {{ formatDate(order.createdAt) }}
            </p>
          </div>
          <span class="inline-flex shrink-0 items-center gap-1 text-sm font-black" :class="statusInfo(order).text">
            <i class="mdi" :class="statusInfo(order).icon"></i>
            {{ statusInfo(order).label }}
          </span>
        </div>

        <!-- One short note when something needs attention -->
        <p
          v-if="isAwaitingPayment(order) || isPaymentExpired(order)"
          class="border-b border-[#E8D9C9]/70 px-4 py-2 text-xs font-bold sm:px-5"
          :class="isPaymentExpired(order) ? 'text-slate-500' : 'text-amber-700'"
        >
          {{ paymentDeadlineText(order) }}
        </p>
        <p
          v-else-if="order.shipment || order.orderStatus === 'Shipped'"
          class="border-b border-[#E8D9C9]/70 px-4 py-2 text-xs font-bold text-sky-700 sm:px-5"
        >
          {{ order.shipment?.shippingProvider || 'ขนส่ง' }} · เลขพัสดุ
          {{ order.shipment?.trackingNumber || 'รอร้านแจ้ง' }}
        </p>

        <!-- Items -->
        <div class="divide-y divide-[#E8D9C9]/60 px-4 sm:px-5">
          <div v-if="!order.items.length" class="flex items-center justify-between py-3 text-sm font-semibold text-[#786B62]">
            <span>คำสั่งซื้อสินค้า #{{ order.orderNumber }}</span>
          </div>
          <div v-for="item in order.items" :key="item.orderItemId" class="flex items-center gap-3 py-3">
            <div class="h-14 w-14 shrink-0 overflow-hidden rounded-lg bg-[#F7F0E6]">
              <img v-if="item.imageUrl" :src="imageUrl(item.imageUrl)" :alt="item.productName" class="h-full w-full object-cover" />
              <div v-else class="flex h-full w-full items-center justify-center text-[#C9B8A8]">
                <i class="mdi mdi-shopping-outline text-xl"></i>
              </div>
            </div>
            <div class="min-w-0 flex-1">
              <p class="line-clamp-1 text-sm font-bold text-[#332820]">{{ item.productName }}</p>
              <p class="text-xs font-semibold text-[#786B62]">x{{ item.quantity }}</p>
            </div>
            <span class="shrink-0 text-sm font-bold text-[#332820]">฿{{ Number(item.unitPrice).toLocaleString('th-TH') }}</span>
          </div>
        </div>

        <!-- Total + actions -->
        <div class="flex flex-wrap items-center justify-between gap-3 border-t border-[#E8D9C9]/70 px-4 py-3 sm:px-5">
          <p class="text-sm font-semibold text-[#786B62]">
            รวม <span class="text-lg font-black text-[#332820]">฿{{ Number(order.totalAmount).toLocaleString('th-TH') }}</span>
          </p>

          <div v-if="!isPaymentExpired(order)" class="flex flex-wrap items-center gap-2">
            <RouterLink
              :to="`/orders/${order.orderId}`"
              class="rounded-full border border-[#E8D9C9] px-4 py-2 text-sm font-black text-[#332820] transition hover:border-[#D96C2C] hover:text-[#D96C2C]"
            >
              ดูรายละเอียด
            </RouterLink>

            <RouterLink
              v-if="order.paymentStatus !== 'Paid' && order.paymentStatus !== 'PendingVerification' && order.orderStatus !== 'Cancelled'"
              :to="`/orders/${order.orderId}/pay`"
              class="rounded-full bg-[#D96C2C] px-5 py-2 text-sm font-black text-white transition hover:bg-[#BF5720]"
            >
              <span class="text-white">ชำระเงิน</span>
            </RouterLink>
            <RouterLink
              v-else-if="order.orderStatus === 'Shipped'"
              :to="`/orders/${order.orderId}`"
              class="rounded-full bg-sky-600 px-5 py-2 text-sm font-black text-white transition hover:bg-sky-700"
            >
              <span class="text-white">ติดตามพัสดุ</span>
            </RouterLink>
            <RouterLink
              v-else-if="order.orderStatus === 'Completed' && order.shopId"
              :to="`/shops/${order.shopId}`"
              class="rounded-full bg-[#D96C2C] px-5 py-2 text-sm font-black text-white transition hover:bg-[#BF5720]"
            >
              <span class="text-white">ซื้ออีกครั้ง</span>
            </RouterLink>
          </div>
          <span v-else class="text-sm font-bold text-slate-500">หมดเวลาชำระเงิน กรุณาสั่งซื้อใหม่</span>
        </div>
      </article>
    </div>
  </div>
</template>