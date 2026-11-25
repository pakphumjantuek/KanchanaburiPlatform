<script setup lang="ts">
import { ref } from 'vue'
import AppTextField from '@/components/common/input/AppTextField.vue'

const props = defineProps<{
  initialProvider?: string
  initialTracking?: string
  presetProviders?: string[]
  updating?: boolean
}>()

const emit = defineEmits<{
  (e: 'submit', payload: { provider: string; trackingNumber: string }): void
}>()

const defaultPresets = [
  'Flash Express',
  'Kerry Express',
  'ไปรษณีย์ไทย (EMS)',
  'J&T Express',
  'Ninja Van',
]

const provider = ref(props.initialProvider || 'Flash Express')
const trackingNumber = ref(props.initialTracking || '')

function handleSubmit() {
  emit('submit', {
    provider: provider.value.trim(),
    trackingNumber: trackingNumber.value.trim(),
  })
}
</script>

<template>
  <div class="rounded-2xl border border-emerald-200 bg-gradient-to-r from-emerald-50/40 to-teal-50/20 p-4.5 shadow-2xs space-y-3.5">
    <div class="flex items-center justify-between border-b border-emerald-100 pb-2.5">
      <div class="flex items-center gap-2">
        <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-emerald-600 text-white text-sm">
          <i class="mdi mdi-truck-fast text-white"></i>
        </span>
        <span class="font-bold text-sm text-slate-900">กรอกข้อมูลการจัดส่งพัสดุ</span>
      </div>
      <span class="text-[11px] font-semibold text-slate-500">บันทึกเลขพัสดุเพื่อแจ้งลูกค้า</span>
    </div>

    <!-- Preset Delivery Company Chips -->
    <div class="flex items-center gap-1.5 flex-wrap">
      <span class="text-xs font-semibold text-slate-500 mr-1">ขนส่งยอดนิยม:</span>
      <button
        v-for="preset in presetProviders || defaultPresets"
        :key="preset"
        type="button"
        class="px-2.5 py-1 rounded-lg text-xs font-bold transition border cursor-pointer select-none"
        :class="
          provider === preset
            ? 'bg-emerald-600 text-white border-emerald-600 shadow-2xs'
            : 'bg-white text-slate-700 border-slate-200 hover:border-emerald-300 hover:bg-emerald-50/50'
        "
        @click="provider = preset"
      >
        {{ preset }}
      </button>
    </div>

    <!-- Provider & Tracking Input Fields -->
    <div class="grid gap-3 sm:grid-cols-[1fr_1fr_auto] sm:items-end pt-1">
      <AppTextField
        v-model="provider"
        label="บริษัทขนส่ง *"
        placeholder="เช่น Flash Express, Kerry"
      />
      <AppTextField
        v-model="trackingNumber"
        label="เลขพัสดุ (Tracking Number) *"
        placeholder="เช่น TH123456789"
      />
      <button
        type="button"
        class="inline-flex items-center justify-center gap-1.5 rounded-xl bg-emerald-600 hover:bg-emerald-700 px-5 py-2.5 font-bold text-xs sm:text-sm text-white shadow-xs transition active:scale-95 disabled:opacity-60 cursor-pointer border border-emerald-600"
        :disabled="updating || !trackingNumber.trim()"
        @click="handleSubmit"
      >
        <i :class="['mdi text-base text-white', updating ? 'mdi-loading animate-spin' : 'mdi-truck-check']"></i>
        <span class="text-white">{{ updating ? 'กำลังบันทึก...' : 'ยืนยันการจัดส่ง' }}</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
button,
input {
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
