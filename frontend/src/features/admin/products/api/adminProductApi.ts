import http from '@/shared/api/http'
import type { Product } from '@/features/shops/shared/types/product'
import type { PagedResult } from '@/shared/interface/PagedResult'

export async function getAdminProducts(params: {
  shopId: string
  status?: string | null
  page: number
  pageSize: number
}) {
  const queryParams: Record<string, unknown> = {
    shopId: params.shopId,
    page: params.page,
    pageSize: params.pageSize,
  }

  if (params.status && params.status !== 'All') {
    queryParams.status = params.status
  }

  const { data } = await http.get<PagedResult<Product>>('/products/admin', { params: queryParams })
  return data
}

export async function updateAdminProductStatus(id: string, status: 'Active' | 'Inactive') {
  await http.patch(`/products/${id}/status`, { status })
}

