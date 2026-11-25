<script setup lang="ts">
// Public storefront - Orange + Cream Shopping Cart View
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { checkout, type CartItem } from '../api/cartApi'
import { useCartStore } from '@/stores/cart'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import { getUserAddresses, type UserAddress } from '../api/userAddressApi'

import CartHeroBanner from '../components/cart/CartHeroBanner.vue'
import CartEmptyState from '../components/cart/CartEmptyState.vue'
import CartItemRow from '../components/cart/CartItemRow.vue'
import CartAddressSection from '../components/cart/CartAddressSection.vue'
import CartSummaryCard from '../components/cart/CartSummaryCard.vue'

const cartStore = useCartStore()
const cart = computed(() => cartStore.cart)
const loading = computed(() => cartStore.loading)
const updating = ref<string | null>(null)
const swal = useSwal()
const router = useRouter()
const checkingOut = ref(false)
const receiverName = ref('')
const receiverPhone = ref('')
const shippingAddress = ref('')
const addresses = ref<UserAddress[]>([])
const selectedAddressId = ref<string | null>(null)

// Group cart items by Shop
const groupedItems = computed(() => {
  const groups: Record<string, { shopId: string; shopName: string; items: CartItem[] }> = {}
  for (const item of cart.value.items) {
    const sId = item.shopId || 'default'
    if (!groups[sId]) {
      groups[sId] = {
        shopId: item.shopId,
        shopName: item.shopName || 'ร้านค้าชุมชน',
        items: [],
      }
    }
    groups[sId].items.push(item)
  }
  return Object.values(groups)
})

const total = computed(() => cartStore.total)
const itemCount = computed(() => cartStore.itemCount)
const shopCount = computed(() => groupedItems.value.length)
const shippingFeePerShop = 50
const shippingFee = computed(() => shopCount.value * shippingFeePerShop)
const grandTotal = computed(() => total.value + shippingFee.value)

async function load() {
  try {
    const [_, savedAddresses] = await Promise.all([cartStore.load(), getUserAddresses()])
    addresses.value = savedAddresses
    const defaultAddress = savedAddresses.find((address) => address.isDefault) ?? savedAddresses[0]
    if (defaultAddress) selectAddress(defaultAddress)
  } catch (error) {
    await swal.error('โหลดตะกร้าไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

function selectAddress(address: UserAddress) {
  selectedAddressId.value = address.userAddressId
  receiverName.value = address.recipientName
  receiverPhone.value = address.recipientPhone
  shippingAddress.value = [
    address.addressLine,
    address.subDistrict,
    address.district,
    address.province,
    address.postalCode,
  ]
    .filter(Boolean)
    .join(' ')
}

async function setQuantity(id: string, quantity: number) {
  updating.value = id
  try {
    await cartStore.setQuantity(id, quantity)
  } catch (error) {
    await swal.error('แก้ไขจำนวนไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    updating.value = null
  }
}

async function remove(id: string) {
  const result = await swal.confirm('ยืนยันลบสินค้า?', 'ต้องการลบสินค้านี้ออกจากตะกร้าใช่หรือไม่')
  if (!result.isConfirmed) return

  try {
    await cartStore.remove(id)
    await swal.success('ลบสินค้าออกจากตะกร้าแล้ว')
  } catch (error) {
    await swal.error('ลบสินค้าไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

async function placeOrder() {
  if (!cart.value.items.length) return
  if (!receiverName.value || !receiverPhone.value || !shippingAddress.value) {
    await swal.warning('กรอกข้อมูลจัดส่งไม่ครบ', 'กรุณาระบุชื่อผู้รับ เบอร์โทร และที่อยู่จัดส่ง')
    return
  }

  checkingOut.value = true
  try {
    const res = await checkout({
      shippingFee: 50,
      receiverName: receiverName.value || undefined,
      receiverPhone: receiverPhone.value || undefined,
      shippingAddress: shippingAddress.value || undefined,
    })
    cartStore.clear()
    const orders = Array.isArray(res) ? res : [res]
    const orderIds = orders.map((o: { orderId?: string }) => o.orderId).filter(Boolean) as string[]

    await swal.success(
      'สร้างคำสั่งซื้อสำเร็จ!',
      orders.length > 1
        ? `กำลังนำคุณไปที่หน้าชำระเงินรวมสำหรับ ${orders.length} ร้านค้า...`
        : 'กำลังนำคุณไปที่หน้าชำระเงิน...',
    )

    if (orderIds.length === 1) {
      await router.push(`/orders/${orderIds[0]}/pay`)
    } else if (orderIds.length > 1) {
      await router.push({ path: '/orders/pay', query: { orderIds: orderIds.join(',') } })
    } else {
      await router.push('/orders')
    }
  } catch (error) {
    await swal.error(
      'สร้างคำสั่งซื้อไม่สำเร็จ',
      getApiErrorMessage(error, 'กรุณาตรวจสอบสินค้าและจำนวนคงเหลืออีกครั้ง'),
    )
  } finally {
    checkingOut.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="min-h-screen bg-[#F7F0E6] pb-24 text-[#332820]">
    <CartHeroBanner />

    <main class="mx-auto max-w-[1600px] px-4 py-5 sm:px-6 sm:py-6 lg:px-10">
      <!-- Loading skeleton -->
      <div v-if="loading" class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_26rem]">
        <div class="space-y-4">
          <div v-for="i in 3" :key="i" class="h-36 animate-pulse rounded-3xl border border-[#E8D9C9] bg-white"></div>
        </div>
        <div class="h-96 animate-pulse rounded-3xl border border-[#E8D9C9] bg-white"></div>
      </div>

      <CartEmptyState v-else-if="!cart.items.length" />

      <div v-else class="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_26rem]">
        <!-- Items grouped by shop -->
        <section class="min-w-0 space-y-4">
          <p class="text-sm font-black text-[#332820]">
            สินค้าในตะกร้า
            <span class="ml-1 rounded-full bg-[#FFF1E8] px-2.5 py-0.5 text-xs text-[#BF5720]">
              {{ itemCount }} ชิ้น จาก {{ shopCount }} ร้านค้า
            </span>
          </p>

          <div
            v-for="group in groupedItems"
            :key="group.shopId"
            class="space-y-3 rounded-3xl border border-[#E8D9C9] bg-[#FFF9F2] p-3.5 sm:p-4"
          >
            <div class="flex items-center justify-between gap-3">
              <div class="flex min-w-0 items-center gap-2.5">
                <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-white text-xl text-[#D96C2C]">
                  <i class="mdi mdi-store-outline"></i>
                </span>
                <h3 class="truncate text-base font-black text-[#332820] sm:text-lg">{{ group.shopName }}</h3>
              </div>
              <span class="shrink-0 rounded-full border border-[#E8D9C9] bg-white px-3 py-1 text-xs font-bold text-[#786B62]">
                {{ group.items.length }} รายการ
              </span>
            </div>

            <div class="space-y-3">
              <CartItemRow
                v-for="item in group.items"
                :key="item.cartItemId"
                :item="item"
                :updating="updating === item.cartItemId"
                @update-quantity="setQuantity(item.cartItemId, $event)"
                @remove="remove(item.cartItemId)"
              />
            </div>
          </div>
        </section>

        <!-- Address + summary -->
        <aside class="space-y-4 lg:sticky lg:top-28">
          <div class="rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-6">
            <CartAddressSection
              v-model:receiver-name="receiverName"
              v-model:receiver-phone="receiverPhone"
              v-model:shipping-address="shippingAddress"
              :addresses="addresses"
              :selected-address-id="selectedAddressId"
              @select-address="selectAddress"
            />
          </div>

          <div class="rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-6">
            <h2 class="mb-4 flex items-center gap-2 text-base font-black text-[#332820]">
              <i class="mdi mdi-receipt-text-outline text-xl text-[#D96C2C]"></i>
              สรุปคำสั่งซื้อ
            </h2>
            <CartSummaryCard
              :total="total"
              :shipping-fee="shippingFee"
              :grand-total="grandTotal"
              :checking-out="checkingOut"
              @checkout="placeOrder"
            />
          </div>
        </aside>
      </div>
    </main>
  </div>
</template>
