<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import type { UserAddress } from '../../api/userAddressApi'
import AppTextField from '@/components/common/input/AppTextField.vue'
import AppTextarea from '@/components/common/input/AppTextarea.vue'

interface Props {
  addresses: UserAddress[]
  selectedAddressId: string | null
}

defineProps<Props>()

const receiverName = defineModel<string>('receiverName', { default: '' })
const receiverPhone = defineModel<string>('receiverPhone', { default: '' })
const shippingAddress = defineModel<string>('shippingAddress', { default: '' })

const emit = defineEmits<{
  'select-address': [addr: UserAddress]
}>()

// The address is shown as one compact card; editing happens in a dialog so the cart page never needs an inner scroll
const open = ref(false)
const hasAddress = computed(() => Boolean(receiverName.value && receiverPhone.value && shippingAddress.value))

function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape') open.value = false
}
watch(open, (v) => {
  if (v) window.addEventListener('keydown', onKey)
  else window.removeEventListener('keydown', onKey)
})
onBeforeUnmount(() => window.removeEventListener('keydown', onKey))
</script>

<template>
  <div>
    <div class="mb-3 flex items-center justify-between gap-2">
      <h2 class="flex items-center gap-2 text-base font-black text-[#332820]">
        <i class="mdi mdi-map-marker-outline text-xl text-[#D96C2C]"></i>
        ที่อยู่จัดส่ง
      </h2>
      <button
        type="button"
        class="cursor-pointer rounded-full px-3 py-1 text-xs font-black text-[#D96C2C] transition hover:bg-[#FFF1E8] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
        @click="open = true"
      >
        {{ hasAddress ? 'เปลี่ยน' : 'เพิ่มที่อยู่' }}
      </button>
    </div>

    <!-- Compact summary -->
    <button
      v-if="hasAddress"
      type="button"
      class="block w-full cursor-pointer rounded-2xl bg-[#FFF9F2] p-3.5 text-left transition hover:bg-[#FFF1E8]"
      @click="open = true"
    >
      <span class="block truncate text-sm font-black text-[#332820]">{{ receiverName }} · {{ receiverPhone }}</span>
      <span class="mt-0.5 line-clamp-2 text-xs leading-relaxed font-semibold text-[#786B62]">{{ shippingAddress }}</span>
    </button>
    <button
      v-else
      type="button"
      class="flex w-full cursor-pointer items-center justify-center gap-2 rounded-2xl border-2 border-dashed border-[#D96C2C]/50 bg-[#FFF9F2] py-4 text-sm font-black text-[#D96C2C] transition hover:bg-[#FFF1E8]"
      @click="open = true"
    >
      <i class="mdi mdi-plus-circle-outline text-lg"></i>
      เพิ่มที่อยู่จัดส่ง
    </button>

    <!-- Editor dialog -->
    <Teleport to="body">
      <div
        v-if="open"
        class="fixed inset-0 z-[100] flex items-end justify-center bg-black/50 p-0 backdrop-blur-sm sm:items-center sm:p-4"
        role="dialog"
        aria-modal="true"
        aria-label="ที่อยู่จัดส่ง"
        @click.self="open = false"
      >
        <div class="flex max-h-[92vh] w-full max-w-xl flex-col overflow-hidden rounded-t-3xl bg-white shadow-2xl sm:rounded-3xl">
          <div class="flex items-center justify-between border-b border-[#E8D9C9] px-5 py-4">
            <h3 class="flex items-center gap-2 text-lg font-black text-[#332820]">
              <i class="mdi mdi-map-marker-outline text-xl text-[#D96C2C]"></i>
              ที่อยู่จัดส่ง
            </h3>
            <button
              type="button"
              class="flex h-9 w-9 cursor-pointer items-center justify-center rounded-full text-[#786B62] transition hover:bg-[#F7F0E6]"
              aria-label="ปิด"
              @click="open = false"
            >
              <i class="mdi mdi-close text-xl"></i>
            </button>
          </div>

          <div class="space-y-5 overflow-y-auto px-5 py-5">
            <div v-if="addresses.length" class="space-y-2" role="radiogroup" aria-label="ที่อยู่ที่บันทึกไว้">
              <p class="text-xs font-bold text-[#786B62]">เลือกจากที่อยู่ที่บันทึกไว้</p>
              <button
                v-for="address in addresses"
                :key="address.userAddressId"
                type="button"
                role="radio"
                :aria-checked="selectedAddressId === address.userAddressId"
                class="flex w-full cursor-pointer items-start gap-3 rounded-2xl border-2 p-3.5 text-left transition focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
                :class="
                  selectedAddressId === address.userAddressId
                    ? 'border-[#D96C2C] bg-[#FFF1E8]'
                    : 'border-[#E8D9C9] bg-white hover:border-[#D96C2C]/50'
                "
                @click="emit('select-address', address)"
              >
                <i
                  class="mdi mt-0.5 text-xl"
                  :class="
                    selectedAddressId === address.userAddressId
                      ? 'mdi-radiobox-marked text-[#D96C2C]'
                      : 'mdi-radiobox-blank text-[#C9B8A8]'
                  "
                ></i>
                <span class="min-w-0">
                  <span class="block text-sm font-black text-[#332820]">
                    {{ address.recipientName }} · {{ address.recipientPhone }}
                  </span>
                  <span class="mt-0.5 block text-xs leading-relaxed font-semibold text-[#786B62]">{{
                    [address.addressLine, address.subDistrict, address.district, address.province, address.postalCode]
                      .filter(Boolean)
                      .join(' ')
                  }}</span>
                </span>
              </button>
            </div>

            <div class="space-y-3">
              <p v-if="addresses.length" class="text-xs font-bold text-[#786B62]">หรือแก้ไข/กรอกที่อยู่ใหม่</p>
              <AppTextField v-model="receiverName" label="ชื่อผู้รับ *" placeholder="ชื่อ-นามสกุล" />
              <AppTextField v-model="receiverPhone" label="เบอร์โทรศัพท์ *" placeholder="08xxxxxxxx" />
              <AppTextarea
                v-model="shippingAddress"
                label="ที่อยู่จัดส่ง *"
                placeholder="บ้านเลขที่ หมู่ ซอย ถนน ตำบล อำเภอ จังหวัด รหัสไปรษณีย์"
                :rows="3"
              />
            </div>
          </div>

          <div class="border-t border-[#E8D9C9] bg-[#FFF9F2] px-5 py-4">
            <button
              type="button"
              class="flex w-full cursor-pointer items-center justify-center gap-2 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] py-3 text-sm font-black text-white shadow-lg shadow-[#D96C2C]/25 transition hover:-translate-y-0.5 active:scale-95"
              @click="open = false"
            >
              <i class="mdi mdi-check text-lg text-white"></i>
              <span class="text-white">ใช้ที่อยู่นี้</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
