<script setup lang="ts">
// Top decorative header banner for Shopping Cart
</script>

<template>
  <section class="border-b border-[#E8D9C9] bg-white">
    <div class="mx-auto flex max-w-[1600px] flex-wrap items-center justify-between gap-3 px-4 py-4 sm:px-6 lg:px-10">
      <h1 class="flex items-center gap-2.5 text-xl leading-[1.35] font-black tracking-tight text-[#332820] sm:text-2xl">
        <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-[#FFF1E8] text-2xl text-[#D96C2C]">
          <i class="mdi mdi-cart-outline"></i>
        </span>
        ตะกร้าสินค้า
      </h1>

      <ol class="flex items-center gap-2 text-xs font-black sm:text-sm" aria-label="ขั้นตอนการสั่งซื้อ">
        <li class="flex items-center gap-2 rounded-full bg-[#D96C2C] px-3.5 py-1.5 text-white">
          <span class="flex h-5 w-5 items-center justify-center rounded-full bg-white/25 text-[11px]">1</span>
          ตะกร้า
        </li>
        <i class="mdi mdi-chevron-right text-[#C9B8A8]"></i>
        <li class="flex items-center gap-2 rounded-full border border-[#E8D9C9] px-3.5 py-1.5 text-[#786B62]">
          <span class="flex h-5 w-5 items-center justify-center rounded-full bg-[#F7F0E6] text-[11px]">2</span>
          ชำระเงิน
        </li>
        <i class="mdi mdi-chevron-right text-[#C9B8A8]"></i>
        <li class="flex items-center gap-2 rounded-full border border-[#E8D9C9] px-3.5 py-1.5 text-[#786B62]">
          <span class="flex h-5 w-5 items-center justify-center rounded-full bg-[#F7F0E6] text-[11px]">3</span>
          รับสินค้า
        </li>
      </ol>
    </div>
  </section>
</template>
