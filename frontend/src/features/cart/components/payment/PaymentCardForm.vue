<script setup lang="ts">
interface Props {
  cardNumber: string
  cardExpiry: string
  cardCvc: string
  cardName: string
}

defineProps<Props>()

const emit = defineEmits<{
  'update:cardNumber': [val: string]
  'update:cardExpiry': [val: string]
  'update:cardCvc': [val: string]
  'update:cardName': [val: string]
}>()
</script>

<template>
  <div class="space-y-4 rounded-2xl border border-[#E8D9C9] bg-white p-5">
    <div class="flex items-center justify-between border-b border-[#E8D9C9] pb-3">
      <span class="flex items-center gap-1.5 text-sm font-black text-[#332820]">
        <i class="mdi mdi-credit-card-outline text-lg text-[#D96C2C]"></i>
        ข้อมูลบัตร
      </span>
    </div>

    <div class="space-y-3 text-sm">
      <div>
        <label class="mb-1 block text-xs font-black text-[#332820]">หมายเลขบัตร</label>
        <input
          :value="cardNumber"
          type="text"
          inputmode="numeric"
          class="w-full rounded-xl border border-[#E8D9C9] bg-white p-3 font-mono font-black text-[#332820] outline-none transition focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/10"
          placeholder="4242 4242 4242 4242"
          @input="emit('update:cardNumber', ($event.target as HTMLInputElement).value)"
        />
      </div>

      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="mb-1 block text-xs font-black text-[#332820]">วันหมดอายุ (เดือน/ปี)</label>
          <input
            :value="cardExpiry"
            type="text"
            class="w-full rounded-xl border border-[#E8D9C9] bg-white p-3 font-mono font-black text-[#332820] outline-none transition focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/10"
            placeholder="12/28"
            @input="emit('update:cardExpiry', ($event.target as HTMLInputElement).value)"
          />
        </div>
        <div>
          <label class="mb-1 block text-xs font-black text-[#332820]">รหัส 3 หลักหลังบัตร</label>
          <input
            :value="cardCvc"
            type="text"
            inputmode="numeric"
            class="w-full rounded-xl border border-[#E8D9C9] bg-white p-3 font-mono font-black text-[#332820] outline-none transition focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/10"
            placeholder="123"
            @input="emit('update:cardCvc', ($event.target as HTMLInputElement).value)"
          />
        </div>
      </div>

      <div>
        <label class="mb-1 block text-xs font-black text-[#332820]">ชื่อบนบัตร</label>
        <input
          :value="cardName"
          type="text"
          class="w-full rounded-xl border border-[#E8D9C9] bg-white p-3 font-black text-[#332820] outline-none transition focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/10"
          placeholder="NAME ON CARD"
          @input="emit('update:cardName', ($event.target as HTMLInputElement).value)"
        />
      </div>
    </div>
  </div>
</template>
