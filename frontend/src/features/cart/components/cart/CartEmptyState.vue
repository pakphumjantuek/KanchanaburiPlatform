<script setup lang="ts">
// Empty Cart State Component
</script>

<template>
  <div
    class="mx-auto my-8 flex max-w-2xl flex-col items-center justify-center rounded-3xl border border-[#E8D9C9] bg-white px-6 py-16 text-center"
  >
    <div class="mb-5 flex h-24 w-24 items-center justify-center rounded-full bg-[#FFF1E8] text-[#D96C2C]">
      <i class="mdi mdi-cart-outline text-5xl"></i>
    </div>
    <h2 class="text-2xl leading-[1.35] font-black text-[#332820]">ตะกร้าของคุณยังว่างอยู่</h2>
    <p class="mt-2 max-w-md text-sm font-semibold text-[#786B62]">
      เลือกดูสินค้าขึ้นชื่อและของฝากจากร้านค้าชุมชนในกาญจนบุรี แล้วกดเพิ่มลงตะกร้าได้เลย
    </p>
    <RouterLink
      to="/shops"
      class="mt-6 inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] px-8 py-3.5 text-sm font-black text-white shadow-lg shadow-[#D96C2C]/30 transition hover:-translate-y-0.5 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95"
    >
      <i class="mdi mdi-storefront-outline text-lg text-white"></i>
      <span class="text-white">เลือกซื้อสินค้า</span>
    </RouterLink>
  </div>
</template>
