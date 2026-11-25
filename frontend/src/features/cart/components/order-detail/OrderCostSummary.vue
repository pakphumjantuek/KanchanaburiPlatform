<script setup lang="ts">
interface Props {
  subtotal: number
  shippingFee: number
  totalAmount: number
  orderId: string
  canPay: boolean
  canConfirmReceipt?: boolean
}

defineEmits<{
  (event: 'confirm-receipt'): void
}>()

defineProps<Props>()
</script>

<template>
  <div class="space-y-4">
    <div class="rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-6">
      <h2 class="mb-4 flex items-center gap-2 text-base font-black text-[#332820]">
        <i class="mdi mdi-receipt-text-outline text-xl text-[#D96C2C]"></i>
        สรุปยอดชำระ
      </h2>
      <div class="space-y-3 text-sm font-semibold">
        <div class="flex justify-between text-[#786B62]">
          <span>ราคาสินค้ารวม</span>
          <span class="font-black text-[#332820]">฿{{ Number(subtotal).toLocaleString('th-TH') }}</span>
        </div>
        <div class="flex justify-between text-[#786B62]">
          <span>ค่าจัดส่ง</span>
          <span class="font-black text-[#332820]">
            {{ Number(shippingFee) > 0 ? `฿${Number(shippingFee).toLocaleString('th-TH')}` : 'ฟรี' }}
          </span>
        </div>
        <div class="flex items-end justify-between border-t border-dashed border-[#E8D9C9] pt-4">
          <span class="text-base font-black text-[#332820]">ยอดรวมสุทธิ</span>
          <span class="text-3xl font-black text-[#D96C2C]">฿{{ Number(totalAmount).toLocaleString('th-TH') }}</span>
        </div>
      </div>

      <RouterLink
        v-if="canPay"
        :to="`/orders/${orderId}/pay`"
        class="mt-5 flex w-full items-center justify-center gap-2 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] px-7 py-3.5 text-sm font-black text-white shadow-lg shadow-[#D96C2C]/30 transition hover:-translate-y-0.5 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95"
      >
        <i class="mdi mdi-credit-card-outline text-lg text-white"></i>
        <span class="text-white">ไปที่หน้าชำระเงิน</span>
      </RouterLink>
    </div>

    <div
      v-if="!canPay && canConfirmReceipt"
      class="space-y-3 rounded-3xl border border-emerald-200 bg-emerald-50 p-5 sm:p-6"
    >
      <div>
        <p class="text-base font-black text-emerald-900">ได้รับสินค้าแล้วหรือไม่?</p>
        <p class="mt-1 text-xs font-semibold text-emerald-800 sm:text-sm">
          ยืนยันเมื่อได้รับสินค้าครบถ้วน เพื่อเปิดสิทธิ์เขียนรีวิวสินค้า
        </p>
      </div>
      <button
        type="button"
        class="flex w-full cursor-pointer items-center justify-center gap-2 rounded-full bg-emerald-600 px-5 py-3 text-sm font-black text-white shadow-lg shadow-emerald-600/25 transition hover:-translate-y-0.5 hover:bg-emerald-700 active:scale-95"
        @click="$emit('confirm-receipt')"
      >
        <i class="mdi mdi-package-check text-lg text-white"></i>
        <span class="text-white">ยืนยันว่าได้รับสินค้าแล้ว</span>
      </button>
    </div>
  </div>
</template>
