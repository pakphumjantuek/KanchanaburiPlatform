<script setup lang="ts">
interface Props {
  total: number
  shippingFee: number
  grandTotal: number
  checkingOut: boolean
}

defineProps<Props>()

const emit = defineEmits<{
  'checkout': []
}>()
</script>

<template>
  <div class="space-y-4">
    <div class="space-y-3 text-sm font-semibold">
      <div class="flex justify-between text-[#786B62]">
        <span>ยอดรวมสินค้า</span>
        <span class="font-black text-[#332820]">฿{{ total.toLocaleString('th-TH') }}</span>
      </div>
      <div class="flex justify-between text-[#786B62]">
        <span>ค่าจัดส่ง</span>
        <span class="font-black text-[#332820]">
          {{ shippingFee > 0 ? `฿${shippingFee.toLocaleString('th-TH')}` : 'ฟรี' }}
        </span>
      </div>
      <div class="flex items-end justify-between border-t border-dashed border-[#E8D9C9] pt-4">
        <span class="text-base font-black text-[#332820]">ยอดรวมทั้งสิ้น</span>
        <span class="text-3xl leading-none font-black text-[#D96C2C]">฿{{ grandTotal.toLocaleString('th-TH') }}</span>
      </div>
    </div>

    <button
      type="button"
      class="flex w-full cursor-pointer items-center justify-center gap-2 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] px-6 py-4 text-base font-black text-white shadow-lg shadow-[#D96C2C]/30 transition hover:-translate-y-0.5 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95 disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0"
      :disabled="checkingOut"
      @click="emit('checkout')"
    >
      <i v-if="checkingOut" class="mdi mdi-loading mdi-spin text-xl text-white"></i>
      <template v-else>
        <i class="mdi mdi-check-circle-outline text-xl text-white"></i>
        <span class="text-white">สั่งซื้อสินค้า</span>
      </template>
    </button>

    <p class="flex items-center justify-center gap-1.5 text-center text-xs font-semibold text-[#786B62]">
      <i class="mdi mdi-shield-check text-base text-emerald-600"></i>
      <span>ขั้นต่อไปคือหน้าชำระเงิน คุณยังตรวจสอบรายการได้อีกครั้ง</span>
    </p>
  </div>
</template>
