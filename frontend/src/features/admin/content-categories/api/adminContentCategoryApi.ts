import http from '@/shared/api/http'
import type { PagedResult } from '@/shared/interface/PagedResult'

export interface ContentCategory {
  contentCategoryId: string
  categoryName: string
  description?: string
  status: 'Active' | 'Inactive'
}

export interface ContentCategoryFormData {
  categoryName: string
  description: string
  status: 'Active' | 'Inactive'
}

export async function getContentCategories(params: {
  status?: string | null
  search?: string | null
  page: number
  pageSize: number
}) {
  const queryParams: Record<string, unknown> = {
    page: params.page,
    pageSize: params.pageSize,
  }
  if (params.status && params.status !== 'All') {
    queryParams.status = params.status
  }
  const { data } = await http.get<PagedResult<ContentCategory>>('/content-categories/admin', {
    params: queryParams,
  })
  if (params.search && params.search.trim()) {
    const q = params.search.toLowerCase().trim()
    const filtered = data.items.filter(
      (c) =>
        c.categoryName.toLowerCase().includes(q) ||
        (c.description && c.description.toLowerCase().includes(q)),
    )
    return {
      ...data,
      items: filtered,
      totalCount: filtered.length,
    }
  }
  return data
}

export async function getContentCategory(id: string) {
  const { data } = await http.get<ContentCategory>(`/content-categories/admin/${id}`)
  return data
}

export async function createContentCategory(
  data: Pick<ContentCategoryFormData, 'categoryName' | 'description'>,
) {
  const { data: result } = await http.post<ContentCategory>('/content-categories', data)
  return result
}

export async function updateContentCategory(id: string, data: ContentCategoryFormData) {
  await http.put(`/content-categories/${id}`, data)
}

export async function deleteContentCategory(id: string) {
  await http.delete(`/content-categories/${id}`)
}

export async function setContentCategoryStatus(
  id: string,
  category: ContentCategory,
  status: 'Active' | 'Inactive',
) {
  if (status === 'Inactive') {
    await http.delete(`/content-categories/${id}`)
  } else {
    await http.put(`/content-categories/${id}`, {
      categoryName: category.categoryName,
      description: category.description ?? '',
      status: 'Active',
    })
  }
}
