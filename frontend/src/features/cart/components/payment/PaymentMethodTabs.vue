<script setup lang="ts">
interface Props {
  currentTab: 'stripe' | 'bank'
}

defineProps<Props>()

const emit = defineEmits<{
  'update:currentTab': [val: 'stripe' | 'bank']
}>()
</script>

<template>
  <div class="grid gap-3 sm:grid-cols-2" role="radiogroup" aria-label="เลือกวิธีชำระเงิน">
    <button
      type="button"
      role="radio"
      :aria-checked="currentTab === 'stripe'"
      class="flex cursor-pointer items-center gap-3 rounded-2xl border-2 p-4 text-left transition focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
      :class="
        currentTab === 'stripe'
          ? 'border-[#D96C2C] bg-[#FFF1E8] shadow-md shadow-[#D96C2C]/10'
          : 'border-[#E8D9C9] bg-white hover:border-[#D96C2C]/50'
      "
      @click="emit('update:currentTab', 'stripe')"
    >
      <span
        class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl text-2xl"
        :class="currentTab === 'stripe' ? 'bg-[#D96C2C] text-white' : 'bg-[#F7F0E6] text-[#D96C2C]'"
      >
        <i class="mdi mdi-credit-card-outline"></i>
      </span>
      <span class="min-w-0 flex-1">
        <span class="block text-sm font-black text-[#332820]">บัตรเครดิต / เดบิต</span>
        <span class="block text-xs font-semibold text-[#786B62]">ชำระทันที ยืนยันอัตโนมัติ</span>
      </span>
      <i
        class="mdi text-2xl"
        :class="currentTab === 'stripe' ? 'mdi-radiobox-marked text-[#D96C2C]' : 'mdi-radiobox-blank text-[#C9B8A8]'"
      ></i>
    </button>

    <button
      type="button"
      role="radio"
      :aria-checked="currentTab === 'bank'"
      class="flex cursor-pointer items-center gap-3 rounded-2xl border-2 p-4 text-left transition focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
      :class="
        currentTab === 'bank'
          ? 'border-[#D96C2C] bg-[#FFF1E8] shadow-md shadow-[#D96C2C]/10'
          : 'border-[#E8D9C9] bg-white hover:border-[#D96C2C]/50'
      "
      @click="emit('update:currentTab', 'bank')"
    >
      <span
        class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl text-2xl"
        :class="currentTab === 'bank' ? 'bg-[#D96C2C] text-white' : 'bg-[#F7F0E6] text-[#D96C2C]'"
      >
        <i class="mdi mdi-bank-transfer"></i>
      </span>
      <span class="min-w-0 flex-1">
        <span class="block text-sm font-black text-[#332820]">โอนผ่านธนาคาร</span>
        <span class="block text-xs font-semibold text-[#786B62]">โอนแล้วแนบสลิป ร้านตรวจสอบให้</span>
      </span>
      <i
        class="mdi text-2xl"
        :class="currentTab === 'bank' ? 'mdi-radiobox-marked text-[#D96C2C]' : 'mdi-radiobox-blank text-[#C9B8A8]'"
      ></i>
    </button>
  </div>
</template>
