import { Routes } from '@angular/router';
import { PRODUCTS_STRINGS } from './products.strings';

/** Rotas de produtos, carregadas sob demanda a partir de app.routes.ts. */
export const PRODUCTS_ROUTES: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./product-list/product-list').then((m) => m.ProductList),
      },
      // Cadastro em edit (sem id) e edição em edit/:id: apagar o id da URL abre um cadastro novo.
      {
        path: 'edit',
        pathMatch: 'full',
        data: { breadcrumb: PRODUCTS_STRINGS.form.newTitle },
        loadComponent: () => import('./product-form/product-form').then((m) => m.ProductForm),
      },
      {
        path: 'edit/:id',
        data: { breadcrumb: PRODUCTS_STRINGS.form.editTitle },
        loadComponent: () => import('./product-form/product-form').then((m) => m.ProductForm),
      },
      {
        path: 'categories',
        data: { breadcrumb: PRODUCTS_STRINGS.categories.breadcrumb },
        loadComponent: () => import('./categories/category-list').then((m) => m.CategoryList),
      },
      {
        path: 'units',
        data: { breadcrumb: PRODUCTS_STRINGS.units.breadcrumb },
        loadComponent: () => import('./units/unit-list').then((m) => m.UnitList),
      },
    ],
  },
];
