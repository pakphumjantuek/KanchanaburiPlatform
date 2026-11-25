<script setup lang="ts">
interface Props {
  currentStepIndex: number
}

defineProps<Props>()
</script>

<template>
  <div class="rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-7">
    <h2 class="mb-6 flex items-center gap-2 text-base font-black text-[#332820]">
      <i class="mdi mdi-truck-fast-outline text-xl text-[#D96C2C]"></i>
      สถานะการดำเนินการ
    </h2>

    <ol class="grid grid-cols-4">
      <li
        v-for="(step, i) in [
          { n: 1, label: 'สั่งซื้อสินค้า', icon: 'mdi-file-document-check-outline' },
          { n: 2, label: 'ชำระเงินแล้ว', icon: 'mdi-credit-card-check-outline' },
          { n: 3, label: 'จัดส่งสินค้า', icon: 'mdi-truck-fast-outline' },
          { n: 4, label: 'ได้รับสินค้า', icon: 'mdi-check-decagram-outline' },
        ]"
        :key="step.n"
        class="relative flex flex-col items-center text-center"
      >
        <!-- connector line to the next step -->
        <span
          v-if="i < 3"
          class="absolute top-5 left-1/2 h-1 w-full rounded-full"
          :class="currentStepIndex > step.n ? 'bg-[#D96C2C]' : 'bg-[#E8D9C9]'"
        ></span>

        <span
          class="relative z-10 flex h-10 w-10 items-center justify-center rounded-full text-xl transition-all"
          :class="
            currentStepIndex >= step.n
              ? step.n === 4
                ? 'bg-emerald-600 text-white shadow-md shadow-emerald-600/30'
                : 'bg-[#D96C2C] text-white shadow-md shadow-[#D96C2C]/30'
              : 'border border-[#E8D9C9] bg-[#F7F0E6] text-[#A8988C]'
          "
        >
          <i class="mdi" :class="currentStepIndex > step.n || (step.n === 4 && currentStepIndex >= 4) ? 'mdi-check' : step.icon"></i>
        </span>
        <span
          class="mt-2 px-1 text-[11px] leading-snug font-black sm:text-sm"
          :class="currentStepIndex >= step.n ? 'text-[#332820]' : 'text-[#A8988C]'"
          >{{ step.label }}</span
        >
      </li>
    </ol>
  </div>
</template>
