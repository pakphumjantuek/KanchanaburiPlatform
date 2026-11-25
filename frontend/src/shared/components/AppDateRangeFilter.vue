<script setup lang="ts">
export type DatePreset = 'all' | 'today' | '7days' | '30days' | 'custom'

const preset = defineModel<DatePreset>('preset', { default: 'all' })
const startDate = defineModel<string>('startDate', { default: '' })
const endDate = defineModel<string>('endDate', { default: '' })

const emit = defineEmits<{
  (e: 'clear'): void
}>()

function handleClear() {
  preset.value = 'all'
  startDate.value = ''
  endDate.value = ''
  emit('clear')
}
</script>

<template>
  <div
    class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-xs"
  >
    <div class="flex items-center gap-2 flex-wrap">
      <span class="font-bold text-slate-700 flex items-center gap-1.5 mr-1">
        <i class="mdi mdi-calendar-range text-emerald-600 text-base"></i>
        <span>ช่วงเวลา:</span>
      </span>

      <!-- Date Preset Quick Buttons -->
      <button
        type="button"
        class="px-3 py-1.5 rounded-xl font-bold transition border cursor-pointer select-none"
        :class="
          preset === 'all'
            ? 'bg-emerald-600 text-white border-emerald-600 shadow-2xs'
            : 'bg-white text-slate-600 border-slate-200 hover:border-slate-300 hover:bg-slate-50'
        "
        @click="handleClear"
      >
        ทุกช่วงเวลา
      </button>

      <button
        type="button"
        class="px-3 py-1.5 rounded-xl font-bold transition border cursor-pointer select-none"
        :class="
          preset === 'today'
            ? 'bg-emerald-600 text-white border-emerald-600 shadow-2xs'
            : 'bg-white text-slate-600 border-slate-200 hover:border-slate-300 hover:bg-slate-50'
        "
        @click="preset = 'today'"
      >
        วันนี้
      </button>

      <button
        type="button"
        class="px-3 py-1.5 rounded-xl font-bold transition border cursor-pointer select-none"
        :class="
          preset === '7days'
            ? 'bg-emerald-600 text-white border-emerald-600 shadow-2xs'
            : 'bg-white text-slate-600 border-slate-200 hover:border-slate-300 hover:bg-slate-50'
        "
        @click="preset = '7days'"
      >
        7 วันล่าสุด
      </button>

      <button
        type="button"
        class="px-3 py-1.5 rounded-xl font-bold transition border cursor-pointer select-none"
        :class="
          preset === '30days'
            ? 'bg-emerald-600 text-white border-emerald-600 shadow-2xs'
            : 'bg-white text-slate-600 border-slate-200 hover:border-slate-300 hover:bg-slate-50'
        "
        @click="preset = '30days'"
      >
        30 วันล่าสุด
      </button>

      <button
        type="button"
        class="px-3 py-1.5 rounded-xl font-bold transition border cursor-pointer select-none"
        :class="
          preset === 'custom'
            ? 'bg-emerald-600 text-white border-emerald-600 shadow-2xs'
            : 'bg-white text-slate-600 border-slate-200 hover:border-slate-300 hover:bg-slate-50'
        "
        @click="preset = 'custom'"
      >
        ระบุช่วงวันที่...
      </button>
    </div>

    <!-- Custom Date Input Controls -->
    <div
      v-if="preset === 'custom'"
      class="flex items-center gap-2 bg-white p-1.5 rounded-xl border border-slate-200 shadow-2xs"
    >
      <input
        v-model="startDate"
        type="date"
        class="rounded-lg border border-slate-200 px-2.5 py-1 text-xs font-semibold text-slate-800 outline-none focus:border-emerald-600"
      />
      <span class="font-bold text-slate-400">ถึง</span>
      <input
        v-model="endDate"
        type="date"
        class="rounded-lg border border-slate-200 px-2.5 py-1 text-xs font-semibold text-slate-800 outline-none focus:border-emerald-600"
      />
      <button
        type="button"
        class="text-xs font-bold text-rose-600 hover:text-rose-700 px-1.5 cursor-pointer"
        @click="handleClear"
      >
        ล้าง
      </button>
    </div>
  </div>
</template>
