<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { loadStripe, type Stripe, type StripeElements, type StripePaymentElement } from '@stripe/stripe-js'
import http from '@/shared/api/http'
import { useSwal } from '@/plugins/sweetalert'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'

import PaymentMethodTabs from '../components/payment/PaymentMethodTabs.vue'
import PaymentCardForm from '../components/payment/PaymentCardForm.vue'
import PaymentBankTransfer from '../components/payment/PaymentBankTransfer.vue'

interface OrderSummaryItem {
  orderId: string
  orderNumber: string
  shopId: string
  shopName?: string
  subtotal: number
  shippingFee: number
  totalAmount: number
  paymentStatus: string
}

interface BatchOrderSummary {
  orders: OrderSummaryItem[]
  totalAmount: number
  isAllPaid: boolean
}

const route = useRoute()
const router = useRouter()
const swal = useSwal()
const loading = ref(true)
const paying = ref(false)
const errorMessage = ref('')
const paymentTab = ref<'stripe' | 'bank'>('stripe') // Default to Stripe

const summary = ref<BatchOrderSummary | null>(null)
// Slip already sent: nothing left to pay, the admin is checking it
const awaitingVerification = computed(() => !!summary.value?.orders.length && summary.value.orders.every((o) => o.paymentStatus === 'PendingVerification'))
const singleOrderId = computed(() => (route.params.id as string) || '')
const queryOrderIds = computed(() => {
  const q = route.query.orderIds as string
  if (q) return q.split(',').map((s) => s.trim()).filter(Boolean)
  return singleOrderId.value ? [singleOrderId.value] : []
})

// Stripe Real Integration State
let stripe: Stripe | null = null
let elements: StripeElements | null = null
let paymentElement: StripePaymentElement | null = null
const isStripeElementMounted = ref(false)

// Form Card Fields (for fallback test mode)
const cardNumber = ref('4242 4242 4242 4242')
const cardExpiry = ref('12/28')
const cardCvc = ref('123')
const cardName = ref('TEST CARD USER')

onMounted(async () => {
  const ids = queryOrderIds.value
  if (!ids.length) {
    loading.value = false
    return
  }

  // Load summary of orders
  try {
    if (ids.length === 1 && singleOrderId.value) {
      const { data: ord } = await http.get(`/orders/${singleOrderId.value}`)
      summary.value = {
        orders: [
          {
            orderId: ord.orderId,
            orderNumber: ord.orderNumber,
            shopId: ord.shopId,
            shopName: ord.shopName,
            subtotal: ord.subtotal,
            shippingFee: ord.shippingFee,
            totalAmount: ord.totalAmount,
            paymentStatus: ord.paymentStatus,
          },
        ],
        totalAmount: ord.totalAmount,
        isAllPaid: ord.paymentStatus === 'Paid',
      }
    } else {
      const { data } = await http.get<BatchOrderSummary>(`/payments/orders/summary?orderIds=${ids.join(',')}`)
      summary.value = data
    }
  } catch (error) {
    console.warn('Failed to load order summary:', error)
  }

  const publishableKey = import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY
  if (!publishableKey) {
    loading.value = false
    return
  }
  try {
    stripe = await loadStripe(publishableKey)
    if (!stripe) throw new Error('ไม่สามารถโหลด Stripe ได้')

    let clientSecret = ''
    if (ids.length === 1 && singleOrderId.value) {
      const { data } = await http.post<{ clientSecret: string }>(`/payments/orders/${singleOrderId.value}/intent`)
      clientSecret = data?.clientSecret
    } else {
      const { data } = await http.post<{ clientSecret: string }>('/payments/batch/intent', { orderIds: ids })
      clientSecret = data?.clientSecret
    }

    if (clientSecret) {
      elements = stripe.elements({ clientSecret })
      paymentElement = elements.create('payment', {
        layout: 'tabs',
        defaultValues: { billingDetails: { name: 'Customer' } },
      })
      loading.value = false
      await nextTick()
      paymentElement.mount('#stripe-payment-element')
      isStripeElementMounted.value = true
    } else {
      loading.value = false
    }
  } catch {
    loading.value = false
  }
})

onBeforeUnmount(() => paymentElement?.destroy())

async function syncPaymentStatus() {
  const ids = queryOrderIds.value
  if (ids.length === 1 && singleOrderId.value) {
    await http.post(`/payments/orders/${singleOrderId.value}/sync`)
  } else {
    await http.post('/payments/batch/sync', { orderIds: ids })
  }
}

// stripePaid = true: Stripe ตัดเงินสำเร็จแล้ว จึงต้องไม่ปล่อยให้ผู้ใช้ค้างอยู่หน้านี้
// ลองซิงก์สถานะกับระบบสูงสุด 3 ครั้ง ถ้ายังไม่สำเร็จก็พาไปหน้าคำสั่งซื้อพร้อมแจ้งให้ตรวจสอบสถานะ
async function confirmPaymentSuccess(stripePaid = false) {
  const ids = queryOrderIds.value

  if (stripePaid) {
    let synced = false
    for (let attempt = 0; attempt < 3 && !synced; attempt++) {
      try {
        await syncPaymentStatus()
        synced = true
      } catch (error) {
        console.warn(`Sync payment failed (attempt ${attempt + 1})`, error)
        if (attempt < 2) await new Promise((resolve) => setTimeout(resolve, 1500))
      }
    }
    if (synced) {
      await swal.success(
        'ชำระเงินผ่าน Stripe สำเร็จ!',
        ids.length > 1
          ? `ขอบคุณสำหรับการสั่งซื้อ ระบบได้รับชำระเงินของทั้ง ${ids.length} ออเดอร์เรียบร้อยแล้ว`
          : 'ขอบคุณสำหรับการสั่งซื้อ ระบบได้รับชำระเงินเรียบร้อยแล้ว',
      )
    } else {
      await swal.warning(
        'ชำระเงินแล้ว กำลังตรวจสอบสถานะ',
        'ระบบได้รับการชำระเงินจาก Stripe แล้ว แต่ยังอัปเดตสถานะออเดอร์ไม่สำเร็จ กรุณารอสักครู่แล้วตรวจสอบในรายการคำสั่งซื้อ หากสถานะยังไม่เปลี่ยนให้ติดต่อผู้ดูแลระบบ',
      )
    }
    await router.push('/orders')
    return
  }

  await syncPaymentStatus()
  await swal.success(
    'ชำระเงินผ่าน Stripe สำเร็จ!',
    ids.length > 1
      ? `ขอบคุณสำหรับการสั่งซื้อ ระบบได้รับชำระเงินของทั้ง ${ids.length} ออเดอร์เรียบร้อยแล้ว`
      : 'ขอบคุณสำหรับการสั่งซื้อ ระบบได้รับชำระเงินเรียบร้อยแล้ว',
  )
  await router.push('/orders')
}

async function payWithStripe() {
  paying.value = true
  errorMessage.value = ''

  if (stripe && elements && isStripeElementMounted.value) {
    try {
      const result = await stripe.confirmPayment({
        elements,
        confirmParams: { return_url: `${window.location.origin}${import.meta.env.BASE_URL}orders?orderIds=${encodeURIComponent(queryOrderIds.value.join(','))}` },
        redirect: 'if_required',
      })
      if (result.error) {
        errorMessage.value = result.error.message ?? 'ชำระเงินผ่าน Stripe ไม่สำเร็จ'
        paying.value = false
        return
      }
      if (result.paymentIntent?.status === 'succeeded' || result.paymentIntent?.status === 'processing') {
        await confirmPaymentSuccess(true)
        return
      }
    } catch (error) {
      console.warn('Stripe confirm error fallback:', error)
    }
  }

  try {
    await confirmPaymentSuccess()
  } catch (error) {
    errorMessage.value = getApiErrorMessage(error, 'ไม่สามารถทำรายการชำระเงินได้')
  } finally {
    paying.value = false
  }
}

async function handleBankTransferSubmit(file: File, transferTime: string) {
  paying.value = true
  errorMessage.value = ''

  try {
    const ids = queryOrderIds.value

    for (const orderId of ids) {
      const formData = new FormData()
      formData.append('slipFile', file)
      formData.append('transferTime', transferTime)
      await http.post(`/orders/${orderId}/upload-slip`, formData)
    }

    await swal.success(
      'แจ้งชำระเงินเรียบร้อยแล้ว!',
      ids.length > 1
        ? `ระบบได้รับสลิปการโอนเงินของทั้ง ${ids.length} ออเดอร์เรียบร้อยแล้ว ร้านค้าจะทำการตรวจสอบและอนุมัติโดยเร็วที่สุด`
        : 'ระบบได้รับสลิปการโอนเงินเรียบร้อยแล้ว ร้านค้าจะทำการตรวจสอบและอนุมัติโดยเร็วที่สุด',
    )
    await router.push('/orders')
  } catch (error) {
    errorMessage.value = getApiErrorMessage(error, 'ไม่สามารถส่งหลักฐานการโอนเงินได้')
  } finally {
    paying.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-[#F7F0E6] pb-20 text-[#332820]">
    <!-- Top bar: back + steps -->
    <div class="border-b border-[#E8D9C9] bg-white">
      <div class="mx-auto flex max-w-[1400px] flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6 lg:px-10">
        <RouterLink
          :to="singleOrderId ? `/orders/${singleOrderId}` : '/orders'"
          class="inline-flex items-center gap-2 rounded-full border border-[#E8D9C9] bg-white px-4 py-2 text-xs font-black text-[#332820] transition hover:border-[#D96C2C] hover:text-[#D96C2C] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none sm:text-sm"
        >
          <i class="mdi mdi-arrow-left text-base text-[#D96C2C]"></i>
          <span>ย้อนกลับ</span>
        </RouterLink>

        <ol class="flex items-center gap-2 text-xs font-black sm:text-sm" aria-label="ขั้นตอนการสั่งซื้อ">
          <li class="flex items-center gap-2 rounded-full border border-emerald-200 bg-emerald-50 px-3.5 py-1.5 text-emerald-700">
            <i class="mdi mdi-check-circle"></i>
            ตะกร้า
          </li>
          <i class="mdi mdi-chevron-right text-[#C9B8A8]"></i>
          <li class="flex items-center gap-2 rounded-full bg-[#D96C2C] px-3.5 py-1.5 text-white">
            <span class="flex h-5 w-5 items-center justify-center rounded-full bg-white/25 text-[11px]">2</span>
            ชำระเงิน
          </li>
          <i class="mdi mdi-chevron-right text-[#C9B8A8]"></i>
          <li class="flex items-center gap-2 rounded-full border border-[#E8D9C9] px-3.5 py-1.5 text-[#786B62]">
            <span class="flex h-5 w-5 items-center justify-center rounded-full bg-[#F7F0E6] text-[11px]">3</span>
            รับสินค้า
          </li>
        </ol>
      </div>
    </div>

    <main class="mx-auto max-w-[1400px] px-4 py-6 sm:px-6 sm:py-8 lg:px-10">
      <div class="mb-6">
        <h1 class="text-2xl leading-[1.35] font-black text-[#332820] sm:text-3xl">
          {{ (summary?.orders.length ?? 0) > 1 ? 'ชำระเงินหลายรายการ' : 'ชำระเงิน' }}
        </h1>
        <p class="text-sm font-semibold text-[#786B62]">เลือกวิธีชำระเงินที่สะดวก แล้วทำตามขั้นตอนด้านล่าง</p>
      </div>

      <div class="grid items-start gap-6 lg:grid-cols-[22rem_minmax(0,1fr)] xl:grid-cols-[24rem_minmax(0,1fr)] lg:gap-8">
        <!-- RIGHT (wide): choose method + pay -->
        <section class="order-2 min-w-0 space-y-5 lg:order-2">
          <!-- Slip already submitted -->
          <div
            v-if="awaitingVerification"
            class="space-y-4 rounded-3xl border border-violet-200 bg-violet-50 p-6 text-center sm:p-8"
          >
            <div class="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-white text-4xl text-violet-600 shadow-sm">
              <i class="mdi mdi-file-search-outline"></i>
            </div>
            <h2 class="text-xl font-black text-[#332820]">ส่งสลิปแล้ว รอแอดมินตรวจสอบ</h2>
            <p class="mx-auto max-w-md text-sm font-semibold text-[#786B62]">
              ระบบได้รับหลักฐานการโอนเงินของคุณแล้ว ผู้ดูแลระบบจะตรวจสอบและยืนยันให้ ไม่ต้องชำระซ้ำ
            </p>
            <RouterLink
              :to="singleOrderId ? `/orders/${singleOrderId}` : '/orders'"
              class="inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] px-7 py-3 text-sm font-black text-white shadow-lg shadow-[#D96C2C]/25 transition hover:-translate-y-0.5"
            >
              <i class="mdi mdi-receipt-text-outline text-white"></i>
              <span class="text-white">ดูสถานะคำสั่งซื้อ</span>
            </RouterLink>
          </div>

          <template v-else>
          <div class="space-y-4 rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-6">
            <h2 class="flex items-center gap-2 text-base font-black text-[#332820]">
              <i class="mdi mdi-wallet-outline text-xl text-[#D96C2C]"></i>
              เลือกวิธีชำระเงิน
            </h2>
            <PaymentMethodTabs v-model:current-tab="paymentTab" />
          </div>

          <!-- Card payment -->
          <div v-show="paymentTab === 'stripe'" class="space-y-5 rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-6">
            <div v-if="loading" class="h-32 animate-pulse rounded-2xl border border-[#E8D9C9] bg-[#F7F0E6]" />

            <template v-else>
              <div id="stripe-payment-element" class="min-h-[60px]" />

              <PaymentCardForm
                v-if="!isStripeElementMounted"
                v-model:card-number="cardNumber"
                v-model:card-expiry="cardExpiry"
                v-model:card-cvc="cardCvc"
                v-model:card-name="cardName"
              />

              <p
                v-if="errorMessage"
                class="flex items-start gap-2 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm font-bold text-rose-800"
                role="alert"
              >
                <i class="mdi mdi-alert-circle-outline text-lg"></i>
                <span>{{ errorMessage }}</span>
              </p>

              <button
                type="button"
                class="flex w-full cursor-pointer items-center justify-center gap-2 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] py-4 text-base font-black text-white shadow-lg shadow-[#D96C2C]/30 transition hover:-translate-y-0.5 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95 disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0"
                :disabled="paying"
                @click="payWithStripe"
              >
                <i
                  class="mdi text-xl text-white"
                  :class="{ 'mdi-loading mdi-spin': paying, 'mdi-lock-outline': !paying }"
                ></i>
                <span class="text-white">
                  {{ paying ? 'กำลังชำระเงิน...' : `ชำระเงิน ฿${(summary?.totalAmount ?? 0).toLocaleString('th-TH')}` }}
                </span>
              </button>

              <p class="flex items-center justify-center gap-1.5 text-xs font-semibold text-[#786B62]">
                <i class="mdi mdi-shield-lock-outline text-base text-[#D96C2C]"></i>
                ข้อมูลบัตรถูกเข้ารหัสผ่าน Stripe
              </p>
            </template>
          </div>

          <!-- Bank transfer -->
          <PaymentBankTransfer
            v-if="paymentTab === 'bank'"
            :paying="paying"
            :total-amount="summary?.totalAmount ?? 0"
            @submit-slip="handleBankTransferSubmit"
          />

          <p
            v-if="paymentTab === 'bank' && errorMessage"
            class="flex items-start gap-2 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm font-bold text-rose-800"
            role="alert"
          >
            <i class="mdi mdi-alert-circle-outline text-lg"></i>
            <span>{{ errorMessage }}</span>
          </p>
        </template>
        </section>

        <!-- LEFT: what you are paying for -->
        <aside class="order-1 space-y-4 lg:sticky lg:top-28 lg:order-1">
          <div class="rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-6">
            <h2 class="mb-4 flex items-center gap-2 text-base font-black text-[#332820]">
              <i class="mdi mdi-receipt-text-outline text-xl text-[#D96C2C]"></i>
              รายการที่ชำระ
            </h2>

            <div v-if="summary && summary.orders.length > 0" class="space-y-2.5">
              <div
                v-for="ord in summary.orders"
                :key="ord.orderId"
                class="flex items-center justify-between gap-3 rounded-2xl bg-[#FFF9F2] p-3"
              >
                <div class="min-w-0">
                  <span class="block truncate text-sm font-black text-[#332820]">{{ ord.shopName || 'ร้านค้าชุมชน' }}</span>
                  <span class="text-[11px] font-semibold text-[#786B62]">#{{ ord.orderNumber }}</span>
                </div>
                <span class="shrink-0 text-sm font-black text-[#332820]">฿{{ ord.totalAmount.toLocaleString('th-TH') }}</span>
              </div>
            </div>
            <div v-else class="h-16 animate-pulse rounded-2xl bg-[#F7F0E6]"></div>

            <div class="mt-4 flex items-end justify-between border-t border-dashed border-[#E8D9C9] pt-4">
              <span class="text-base font-black text-[#332820]">ยอดที่ต้องชำระ</span>
              <span class="text-3xl leading-none font-black text-[#D96C2C]">
                ฿{{ (summary?.totalAmount ?? 0).toLocaleString('th-TH') }}
              </span>
            </div>
          </div>

          <p class="flex items-start gap-2 rounded-2xl border border-emerald-200 bg-emerald-50 p-4 text-xs font-semibold text-emerald-800">
            <i class="mdi mdi-clock-outline text-lg"></i>
            <span>กรุณาชำระภายในเวลาที่กำหนด หากเกินเวลา คำสั่งซื้อจะถูกยกเลิกและสินค้าจะคืนเข้าสต็อก</span>
          </p>
        </aside>
      </div>
    </main>
  </div>
</template>
