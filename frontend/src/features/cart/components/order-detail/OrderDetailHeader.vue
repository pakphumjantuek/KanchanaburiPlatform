<script setup lang="ts">
import { computed } from 'vue'
import { formatDate } from '@/shared/utils/formatDate'
interface Props {
  orderNumber: string
  createdAt: string
  paymentStatus: string
  orderStatus: string
  expired?: boolean
  awaitingPayment?: boolean
  pendingVerification?: boolean
  expiresAtMs?: number | null
  now?: number
}

const props = defineProps<Props>()

// One headline that tells the customer what is happening right now
function headline() {
  if (props.expired) return { title: 'หมดเวลาชำระเงินแล้ว', hint: 'สินค้าถูกคืนเข้าสต็อก กรุณาสั่งซื้อใหม่อีกครั้ง', icon: 'mdi-clock-remove-outline', cls: 'bg-slate-100 text-slate-600' }
  if (props.orderStatus === 'Cancelled') return { title: 'ยกเลิกคำสั่งซื้อแล้ว', hint: 'คำสั่งซื้อนี้ถูกยกเลิก', icon: 'mdi-close-circle-outline', cls: 'bg-rose-50 text-rose-600' }
  if (props.orderStatus === 'Completed') return { title: 'ได้รับสินค้าเรียบร้อย', hint: 'ขอบคุณที่อุดหนุนสินค้าชุมชน เขียนรีวิวให้ร้านได้เลย', icon: 'mdi-check-circle-outline', cls: 'bg-emerald-50 text-emerald-600' }
  if (props.orderStatus === 'Shipped') return { title: 'สินค้ากำลังเดินทางมาหาคุณ', hint: 'เมื่อได้รับของแล้ว กดยืนยันรับสินค้าด้านขวา', icon: 'mdi-truck-fast-outline', cls: 'bg-sky-50 text-sky-600' }
  if (props.pendingVerification) return { title: 'ส่งสลิปแล้ว รอแอดมินตรวจสอบ', hint: 'ระบบได้รับหลักฐานการโอนแล้ว ผู้ดูแลระบบจะตรวจสอบและยืนยันให้ ไม่ต้องชำระซ้ำ', icon: 'mdi-file-search-outline', cls: 'bg-violet-50 text-violet-600' }
  if (props.paymentStatus === 'Paid') return { title: 'ร้านกำลังเตรียมสินค้า', hint: 'ชำระเงินแล้ว ร้านจะจัดส่งให้เร็วที่สุด', icon: 'mdi-package-variant', cls: 'bg-[#FFF1E8] text-[#D96C2C]' }
  return { title: 'รอชำระเงิน', hint: 'กรุณาชำระเงินภายในเวลาที่กำหนดเพื่อยืนยันคำสั่งซื้อ', icon: 'mdi-credit-card-clock-outline', cls: 'bg-amber-50 text-amber-600' }
}

// Payment deadline: exact date/time + live countdown
const showDeadline = computed(() => Boolean(props.expiresAtMs) && (props.awaitingPayment || props.expired))
const deadlineText = computed(() =>
  props.expiresAtMs
    ? formatDate(new Date(props.expiresAtMs).toISOString(), { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })
    : '',
)
const remaining = computed(() => {
  const sec = Math.max(0, Math.floor(((props.expiresAtMs ?? 0) - (props.now ?? Date.now())) / 1000))
  const m = String(Math.floor(sec / 60)).padStart(2, '0')
  const ss = String(sec % 60).padStart(2, '0')
  return `${m}:${ss}`
})

function getStatusTitle(status: string) {
  if (status === 'Pending') return 'รอยืนยันคำสั่งซื้อ'
  if (status === 'Confirmed') return 'ร้านกำลังเตรียมจัดส่ง'
  if (status === 'Shipped') return 'จัดส่งสินค้าแล้ว'
  if (status === 'Completed') return 'สำเร็จสมบูรณ์'
  if (status === 'Cancelled') return 'ยกเลิกคำสั่งซื้อ'
  return status
}


</script>

<template>
  <div class="rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-7">
    <!-- Order number + date -->
    <div class="flex flex-wrap items-start justify-between gap-3">
      <div>
        <p class="text-xs font-bold text-[#786B62]">คำสั่งซื้อหมายเลข</p>
        <h1 class="font-mono text-xl font-black text-[#332820] sm:text-2xl">#{{ props.orderNumber }}</h1>
        <p class="mt-1 flex items-center gap-1.5 text-xs font-semibold text-[#786B62] sm:text-sm">
          <i class="mdi mdi-clock-outline text-[#D96C2C]"></i>
          สั่งซื้อเมื่อ
          {{ formatDate(props.createdAt, { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }) }}
        </p>
      </div>

      <span
        class="inline-flex items-center gap-1.5 rounded-full border px-3.5 py-1.5 text-xs font-black sm:text-sm"
        :class="
          props.paymentStatus === 'Paid'
            ? 'border-emerald-200 bg-emerald-50 text-emerald-700'
            : props.pendingVerification
              ? 'border-violet-200 bg-violet-50 text-violet-700'
              : 'border-amber-200 bg-amber-50 text-amber-800'
        "
      >
        <i
          class="mdi"
          :class="
            props.paymentStatus === 'Paid'
              ? 'mdi-check-circle'
              : props.pendingVerification
                ? 'mdi-file-search-outline'
                : 'mdi-clock-outline'
          "
        ></i>
        {{
          props.paymentStatus === 'Paid'
            ? 'ชำระเงินเรียบร้อย'
            : props.pendingVerification
              ? 'รอแอดมินตรวจสอบสลิป'
              : 'รอการชำระเงิน'
        }}
      </span>
    </div>

    <!-- What is happening now -->
    <div class="mt-5 flex items-center gap-4 rounded-2xl p-4 sm:p-5" :class="headline().cls">
      <span class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-white text-3xl shadow-sm">
        <i class="mdi" :class="headline().icon"></i>
      </span>
      <div class="min-w-0">
        <p class="text-lg leading-[1.35] font-black text-[#332820] sm:text-xl">{{ headline().title }}</p>
        <p class="text-xs font-semibold text-[#786B62] sm:text-sm">{{ headline().hint }}</p>
      </div>
    </div>
    <!-- Payment deadline -->
    <div
      v-if="showDeadline"
      class="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-2xl border px-4 py-3"
      :class="props.expired ? 'border-slate-200 bg-slate-50' : 'border-amber-200 bg-amber-50'"
    >
      <div class="flex items-center gap-3">
        <i class="mdi text-2xl" :class="props.expired ? 'mdi-clock-remove-outline text-slate-500' : 'mdi-clock-alert-outline text-amber-600'"></i>
        <div>
          <p class="text-xs font-bold" :class="props.expired ? 'text-slate-600' : 'text-amber-800'">
            {{ props.expired ? 'หมดเวลาชำระเงินเมื่อ' : 'ต้องชำระเงินก่อน' }}
          </p>
          <p class="text-sm font-black text-[#332820] sm:text-base">{{ deadlineText }} น.</p>
        </div>
      </div>
      <div v-if="!props.expired" class="text-right">
        <p class="text-[11px] font-bold text-amber-800">เหลือเวลา</p>
        <p class="font-mono text-2xl leading-none font-black text-amber-700 tabular-nums">{{ remaining }}</p>
      </div>
    </div>
  </div>
</template>
