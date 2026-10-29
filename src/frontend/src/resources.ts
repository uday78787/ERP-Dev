export type FieldKind = "text" | "number" | "checkbox" | "select";

export interface FieldDefinition {
  name: string;
  label: string;
  kind?: FieldKind;
  required?: boolean;
  options?: { value: string; label: string }[];
  optionRoute?: string;
  optionLabel?: string;
  help?: string;
  min?: number;
  max?: number;
  step?: number;
}

export interface ResourceDefinition {
  key: string;
  label: string;
  route: string;
  columns: { key: string; label: string }[];
  fields: FieldDefinition[];
  bomVersions?: boolean;
}

export const resources: ResourceDefinition[] = [
  {
    key: "customers",
    label: "Customers",
    route: "customers",
    columns: [
      { key: "code", label: "Code" },
      { key: "name", label: "Customer" },
      { key: "email", label: "Email" },
      { key: "city", label: "City" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      { name: "code", label: "Customer code", required: true },
      { name: "name", label: "Name", required: true },
      { name: "legalName", label: "Legal name" },
      { name: "email", label: "Email", kind: "text" },
      { name: "phone", label: "Phone" },
      { name: "addressLine1", label: "Address line 1" },
      { name: "addressLine2", label: "Address line 2" },
      { name: "city", label: "City" },
      { name: "stateOrProvince", label: "State / province" },
      { name: "postalCode", label: "Postal code" },
      { name: "countryCode", label: "Country code", help: "ISO 3166-1 alpha-2" },
    ],
  },
  {
    key: "suppliers",
    label: "Suppliers",
    route: "suppliers",
    columns: [
      { key: "code", label: "Code" },
      { key: "name", label: "Supplier" },
      { key: "email", label: "Email" },
      { key: "city", label: "City" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      { name: "code", label: "Supplier code", required: true },
      { name: "name", label: "Name", required: true },
      { name: "legalName", label: "Legal name" },
      { name: "email", label: "Email" },
      { name: "phone", label: "Phone" },
      { name: "addressLine1", label: "Address line 1" },
      { name: "addressLine2", label: "Address line 2" },
      { name: "city", label: "City" },
      { name: "stateOrProvince", label: "State / province" },
      { name: "postalCode", label: "Postal code" },
      { name: "countryCode", label: "Country code", help: "ISO 3166-1 alpha-2" },
    ],
  },
  {
    key: "item-categories",
    label: "Item categories",
    route: "item-categories",
    columns: [
      { key: "code", label: "Code" },
      { key: "name", label: "Category" },
      { key: "categoryType", label: "Material type" },
      { key: "trackingMode", label: "Tracking" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      { name: "code", label: "Category code", required: true },
      { name: "name", label: "Name", required: true },
      {
        name: "categoryType",
        label: "Material type",
        kind: "select",
        required: true,
        options: [
          { value: "general", label: "General" },
          { value: "fabric", label: "Fabric" },
          { value: "trims", label: "Trims" },
          { value: "accessory", label: "Accessory" },
          { value: "packaging", label: "Packaging" },
          { value: "finished_good", label: "Finished good" },
        ],
      },
      {
        name: "trackingMode",
        label: "Tracking mode",
        kind: "select",
        required: true,
        options: [
          { value: "none", label: "None" },
          { value: "lot", label: "Lot" },
          { value: "serial", label: "Serial" },
        ],
      },
      { name: "description", label: "Description" },
    ],
  },
  {
    key: "units",
    label: "Units of measure",
    route: "units",
    columns: [
      { key: "code", label: "Code" },
      { key: "name", label: "Unit" },
      { key: "dimension", label: "Dimension" },
      { key: "decimalPlaces", label: "Precision" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      { name: "code", label: "Unit code", required: true },
      { name: "name", label: "Name", required: true },
      { name: "dimension", label: "Dimension", required: true },
      { name: "decimalPlaces", label: "Decimal places", kind: "number", min: 0, max: 6 },
    ],
  },
  {
    key: "items",
    label: "Items",
    route: "items",
    columns: [
      { key: "code", label: "Code" },
      { key: "name", label: "Item" },
      { key: "categoryName", label: "Category" },
      { key: "stockUnitCode", label: "Stock unit" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      {
        name: "code",
        label: "Item code",
        help: "Leave blank to use the configured company code rule.",
      },
      { name: "name", label: "Item name", required: true },
      {
        name: "categoryId",
        label: "Category",
        kind: "select",
        required: true,
        optionRoute: "item-categories",
        optionLabel: "codeName",
      },
      {
        name: "stockUnitId",
        label: "Stock unit",
        kind: "select",
        required: true,
        optionRoute: "units",
        optionLabel: "codeName",
      },
      {
        name: "purchaseUnitId",
        label: "Purchase unit",
        kind: "select",
        required: true,
        optionRoute: "units",
        optionLabel: "codeName",
      },
      {
        name: "purchaseToStockFactor",
        label: "Purchase-to-stock factor",
        kind: "number",
        min: 0.0000000001,
        step: 0.0001,
        required: true,
      },
      { name: "description", label: "Description" },
    ],
  },
  {
    key: "warehouses",
    label: "Warehouses",
    route: "warehouses",
    columns: [
      { key: "code", label: "Code" },
      { key: "name", label: "Warehouse" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      { name: "code", label: "Warehouse code", required: true },
      { name: "name", label: "Name", required: true },
    ],
  },
  {
    key: "warehouse-locations",
    label: "Warehouse locations",
    route: "warehouse-locations",
    columns: [
      { key: "warehouseCode", label: "Warehouse" },
      { key: "code", label: "Location code" },
      { key: "name", label: "Location" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      {
        name: "warehouseId",
        label: "Warehouse",
        kind: "select",
        required: true,
        optionRoute: "warehouses",
        optionLabel: "codeName",
      },
      {
        name: "parentLocationId",
        label: "Parent location",
        kind: "select",
        optionRoute: "warehouse-locations",
        optionLabel: "codeName",
      },
      { name: "code", label: "Location code", required: true },
      { name: "name", label: "Name", required: true },
    ],
  },
  {
    key: "products",
    label: "Products and styles",
    route: "products",
    columns: [
      { key: "code", label: "Style code" },
      { key: "name", label: "Style / product" },
      { key: "industryCode", label: "Industry" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      { name: "code", label: "Style code", required: true },
      { name: "name", label: "Style name", required: true },
      { name: "industryCode", label: "Industry code", required: true },
      { name: "description", label: "Description" },
    ],
  },
  {
    key: "variants",
    label: "Size / color variants",
    route: "variants",
    columns: [
      { key: "code", label: "Variant code" },
      { key: "name", label: "Variant" },
      { key: "productCode", label: "Style" },
      { key: "sizeCode", label: "Size" },
      { key: "colorName", label: "Color" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      {
        name: "productId",
        label: "Product / style",
        kind: "select",
        required: true,
        optionRoute: "products",
        optionLabel: "codeName",
      },
      {
        name: "itemId",
        label: "Item",
        kind: "select",
        required: true,
        optionRoute: "items",
        optionLabel: "codeName",
      },
      { name: "code", label: "Variant code", required: true },
      { name: "name", label: "Variant name", required: true },
      { name: "sizeCode", label: "Size" },
      { name: "colorCode", label: "Color code" },
      { name: "colorName", label: "Color name" },
    ],
  },
  {
    key: "machines",
    label: "Machines",
    route: "machines",
    columns: [
      { key: "code", label: "Machine code" },
      { key: "name", label: "Machine" },
      { key: "productionLine", label: "Production line" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      { name: "code", label: "Machine code", required: true },
      { name: "name", label: "Name", required: true },
      { name: "productionLine", label: "Production line" },
      { name: "description", label: "Description" },
    ],
  },
  {
    key: "operations",
    label: "Production operations",
    route: "operations",
    columns: [
      { key: "code", label: "Operation code" },
      { key: "name", label: "Operation" },
      { key: "isOutsourced", label: "Outsourced" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      { name: "code", label: "Operation code", required: true },
      { name: "name", label: "Name", required: true },
      { name: "description", label: "Description" },
      { name: "isOutsourced", label: "Outsourced operation", kind: "checkbox" },
    ],
  },
  {
    key: "boms",
    label: "BOMs and versions",
    route: "boms",
    columns: [
      { key: "code", label: "BOM code" },
      { key: "name", label: "BOM" },
      { key: "productCode", label: "Style" },
      { key: "versionCount", label: "Versions" },
      { key: "isActive", label: "Status" },
    ],
    fields: [
      {
        name: "productId",
        label: "Product / style",
        kind: "select",
        required: true,
        optionRoute: "products",
        optionLabel: "codeName",
      },
      { name: "code", label: "BOM code", required: true },
      { name: "name", label: "Name", required: true },
    ],
    bomVersions: true,
  },
];
