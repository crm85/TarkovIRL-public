"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
const config = __importStar(require("../config/config.json"));
class Mod {
    constructor() {
        this.weaponBaseValues = {};
        this.weaponValueLow = 0;
        this.weaponValueHigh = 1;
        this.weaponID = "5422acb9af1c889c16000029";
        this.fenceTraderID = "579dc571d53a0658a154fbec";
        this.roublesID = "5449016a4bdc2d6f028b456f";
        this.ammoBoxID = "543be5cb4bdc2deb348b4568";
        this.expandedFenceInvalidBaseTypes = [
            "566965d44bdc2d814c8b4571",
            "5448bf274bdc2dfc2f8b456a",
            "566abbb64bdc2d144c8b457d",
            "6050cac987d3f925bf016837",
            "55d720f24bdc2d88028b456d",
            "567583764bdc2d98058b456e",
            "557596e64bdc2dc2118b4571",
            "63da6da4784a55176c018dba",
            "62f109593b54472778797866"
        ];
    }
    preSptLoad(container) {
        const logger = container.resolve("WinstonLogger");
        this.patchExpandedFenceAssort(container, logger);
    }
    patchExpandedFenceAssort(container, logger) {
        if (!this.getExpandedFenceSettings().enabled)
            return;
        container.afterResolution("FenceBaseAssortGenerator", (_token, fenceBaseAssortGenerator) => {
            if (!fenceBaseAssortGenerator || fenceBaseAssortGenerator.__breakContactExpandedFenceBasePatched)
                return;
            const originalGenerateFenceBaseAssorts = fenceBaseAssortGenerator.generateFenceBaseAssorts;
            if (typeof originalGenerateFenceBaseAssorts !== "function") {
                this.logWarning(logger, "Break Contact could not patch Fence base assort: generateFenceBaseAssorts was not found.");
                return;
            }
            const mod = this;
            fenceBaseAssortGenerator.generateFenceBaseAssorts = function (...args) {
                const result = originalGenerateFenceBaseAssorts.apply(this, args);
                const addedCount = mod.expandFenceBaseAssort(this, logger);
                if (mod.getExpandedFenceSettings().debugLogging) {
                    logger.info(`Break Contact added ${addedCount} extra item template(s) to Fence's roll pool.`);
                }
                return result;
            };
            fenceBaseAssortGenerator.__breakContactExpandedFenceBasePatched = true;
        }, { frequency: "Always" });
        container.afterResolution("FenceService", (_token, fenceService) => {
            if (!fenceService || fenceService.__breakContactExpandedFenceRollPatched)
                return;
            const originalCreateAssorts = fenceService.createAssorts;
            if (typeof originalCreateAssorts !== "function") {
                this.logWarning(logger, "Break Contact could not patch Fence assort rolls: createAssorts was not found.");
                return;
            }
            const mod = this;
            fenceService.createAssorts = function (...args) {
                if (!mod.getExpandedFenceSettings().enabled) {
                    return originalCreateAssorts.apply(this, args);
                }
                return mod.withExpandedFenceRollConfig(this?.traderConfig?.fence, () => originalCreateAssorts.apply(this, args));
            };
            fenceService.__breakContactExpandedFenceRollPatched = true;
        }, { frequency: "Always" });
    }
    getExpandedFenceSettings() {
        const settings = (config.ExpandedFenceAssort ?? {});
        return {
            enabled: settings.Enabled !== false,
            debugLogging: settings.DebugLogging === true
        };
    }
    withExpandedFenceRollConfig(fenceConfig, action) {
        if (!fenceConfig)
            return action();
        const snapshot = this.captureFenceRollConfig(fenceConfig);
        fenceConfig.blacklist = [];
        fenceConfig.blacklistSeasonalItems = false;
        fenceConfig.ammoMaxPenLimit = Number.MAX_SAFE_INTEGER;
        fenceConfig.itemTypeLimits = {};
        fenceConfig.itemCategoryRoublePriceLimit = {};
        try {
            return action();
        }
        finally {
            this.restoreFenceRollConfig(fenceConfig, snapshot);
        }
    }
    captureFenceRollConfig(fenceConfig) {
        return {
            blacklist: fenceConfig.blacklist,
            blacklistSeasonalItems: fenceConfig.blacklistSeasonalItems,
            ammoMaxPenLimit: fenceConfig.ammoMaxPenLimit,
            itemTypeLimits: fenceConfig.itemTypeLimits,
            itemCategoryRoublePriceLimit: fenceConfig.itemCategoryRoublePriceLimit
        };
    }
    restoreFenceRollConfig(fenceConfig, snapshot) {
        fenceConfig.blacklist = snapshot.blacklist;
        fenceConfig.blacklistSeasonalItems = snapshot.blacklistSeasonalItems;
        fenceConfig.ammoMaxPenLimit = snapshot.ammoMaxPenLimit;
        fenceConfig.itemTypeLimits = snapshot.itemTypeLimits;
        fenceConfig.itemCategoryRoublePriceLimit = snapshot.itemCategoryRoublePriceLimit;
    }
    expandFenceBaseAssort(fenceBaseAssortGenerator, logger) {
        const databaseService = fenceBaseAssortGenerator?.databaseService;
        const baseFenceAssort = databaseService?.getTrader?.(this.fenceTraderID)?.assort;
        const itemHelper = fenceBaseAssortGenerator?.itemHelper;
        const hashUtil = fenceBaseAssortGenerator?.hashUtil;
        const fenceService = fenceBaseAssortGenerator?.fenceService;
        if (!baseFenceAssort || !itemHelper || !hashUtil || !fenceService) {
            this.logWarning(logger, "Break Contact could not expand Fence assort: required SPT services were not available.");
            return 0;
        }
        const items = itemHelper.getItems?.();
        if (!Array.isArray(items)) {
            this.logWarning(logger, "Break Contact could not expand Fence assort: ItemHelper.getItems did not return an array.");
            return 0;
        }
        const existingRootTpls = new Set();
        for (const item of baseFenceAssort.items ?? []) {
            if (item?.parentId === "hideout" && typeof item?._tpl === "string") {
                existingRootTpls.add(item._tpl);
            }
        }
        let added = 0;
        for (const itemDb of items) {
            if (!this.shouldAddExpandedFenceItem(itemDb, itemHelper, existingRootTpls))
                continue;
            const itemWithChildrenToAdd = [
                {
                    _id: hashUtil.generate(),
                    _tpl: itemDb._id,
                    parentId: "hideout",
                    slotId: "hideout",
                    upd: { StackObjectsCount: 9999999 }
                }
            ];
            this.addExpandedFenceItemChildren(fenceBaseAssortGenerator, itemWithChildrenToAdd, itemDb);
            if (!this.hasRequiredExpandedFenceChildren(itemDb, itemWithChildrenToAdd))
                continue;
            itemHelper.remapRootItemId(itemWithChildrenToAdd);
            if (itemWithChildrenToAdd.length > 1) {
                itemHelper.reparentItemAndChildren(itemWithChildrenToAdd[0], itemWithChildrenToAdd);
                itemWithChildrenToAdd[0].parentId = "hideout";
                itemWithChildrenToAdd[0].slotId = "hideout";
            }
            let price = Math.round(this.getExpandedFenceItemPrice(fenceService, itemHelper, itemDb._id, itemWithChildrenToAdd));
            if (!Number.isFinite(price) || price <= 1)
                continue;
            if (price === 100)
                price = 101;
            const rootItem = itemWithChildrenToAdd[0];
            baseFenceAssort.barter_scheme[rootItem._id] = [[{ count: price, _tpl: this.roublesID }]];
            baseFenceAssort.items.push(...itemWithChildrenToAdd);
            baseFenceAssort.loyal_level_items[rootItem._id] = 1;
            existingRootTpls.add(itemDb._id);
            added++;
        }
        return added;
    }
    shouldAddExpandedFenceItem(itemDb, itemHelper, existingRootTpls) {
        if (!itemDb || typeof itemDb._id !== "string" || existingRootTpls.has(itemDb._id))
            return false;
        if (itemDb._type !== "Item")
            return false;
        if (itemDb._props?.QuestItem === true)
            return false;
        const prefabPath = itemDb._props?.Prefab?.path;
        if (typeof prefabPath !== "string" || prefabPath.length === 0)
            return false;
        if (typeof itemHelper.isValidItem === "function") {
            return itemHelper.isValidItem(itemDb._id, this.expandedFenceInvalidBaseTypes);
        }
        return true;
    }
    addExpandedFenceItemChildren(fenceBaseAssortGenerator, itemWithChildren, itemDb) {
        const itemHelper = fenceBaseAssortGenerator?.itemHelper;
        if (!itemHelper)
            return;
        if (this.isItemOfBaseclass(itemHelper, itemDb._id, this.ammoBoxID)) {
            itemHelper.addCartridgesToAmmoBox?.(itemWithChildren, itemDb);
        }
        if (!Array.isArray(itemDb?._props?.Slots) || itemDb._props.Slots.length === 0)
            return;
        if (typeof itemHelper.armorItemCanHoldMods === "function" && itemHelper.armorItemCanHoldMods(itemDb._id)) {
            fenceBaseAssortGenerator.addChildrenToArmorModSlots?.(itemWithChildren, itemDb);
        }
    }
    hasRequiredExpandedFenceChildren(itemDb, itemWithChildren) {
        const requiredSlots = Array.isArray(itemDb?._props?.Slots)
            ? itemDb._props.Slots.filter((slot) => slot?._required === true && typeof slot?._name === "string")
            : [];
        if (requiredSlots.length === 0)
            return true;
        const rootId = itemWithChildren[0]?._id;
        return requiredSlots.every((slot) => itemWithChildren.some((item) => item?.parentId === rootId && item?.slotId === slot._name));
    }
    getExpandedFenceItemPrice(fenceService, itemHelper, itemTpl, itemWithChildren) {
        const fencePrice = Number(fenceService?.getItemPrice?.(itemTpl, itemWithChildren) ?? 0);
        if (Number.isFinite(fencePrice) && fencePrice > 0)
            return fencePrice;
        return Number(itemHelper?.getItemPrice?.(itemTpl) ?? 0);
    }
    isItemOfBaseclass(itemHelper, itemTpl, baseClass) {
        return typeof itemHelper?.isOfBaseclass === "function" && itemHelper.isOfBaseclass(itemTpl, baseClass);
    }
    postDBLoad(container) {
        const logger = container.resolve("WinstonLogger");
        const db = container.resolve("DatabaseServer");
        const tables = db.getTables();
        const items = tables.templates.items;
        if (config.WeaponDurability?.Enabled === false) {
            logger.info("Break Contact weapon durability is disabled by config.");
            return;
        }
        this.configureWeaponDurability(items, tables.templates.handbook);
        const traderWeaponCount = this.applyTraderWeaponDurability(tables.traders, items);
        const locationWeaponCount = this.applyLocationWeaponDurability(tables.locations, items);
        this.patchLocationLootGenerator(container, items, logger);
        logger.success(`Break Contact weapon durability has been applied to ${traderWeaponCount} trader weapons and ${locationWeaponCount} world loot weapon templates!`);
    }
    logWarning(logger, message) {
        const warning = logger.warning ?? logger.error;
        warning.call(logger, message);
    }
    configureWeaponDurability(items, handbook) {
        this.weaponBaseValues = {};
        for (const handbookItem of handbook?.Items ?? []) {
            if (typeof handbookItem?.Id === "string" && typeof handbookItem?.Price === "number") {
                this.weaponBaseValues[handbookItem.Id] = handbookItem.Price;
            }
        }
        const weaponValues = [];
        for (const id in items) {
            if (!this.isWeaponTemplate(id, items))
                continue;
            const templateValue = Number(items[id]?._props?.CreditsPrice ?? 0);
            if (!this.weaponBaseValues[id] && templateValue > 0) {
                this.weaponBaseValues[id] = templateValue;
            }
            const value = this.weaponBaseValues[id] ?? 0;
            if (value > 0) {
                weaponValues.push(value);
            }
        }
        weaponValues.sort((a, b) => a - b);
        this.weaponValueLow = this.percentile(weaponValues, this.normalizePercentile(config.WeaponDurability?.ValuePercentileLow, 0.1));
        this.weaponValueHigh = this.percentile(weaponValues, this.normalizePercentile(config.WeaponDurability?.ValuePercentileHigh, 0.9));
        if (this.weaponValueHigh <= this.weaponValueLow) {
            this.weaponValueHigh = this.weaponValueLow + 1;
        }
    }
    applyTraderWeaponDurability(traders, items) {
        let changed = 0;
        for (const traderId in traders) {
            const assortItems = traders[traderId]?.assort?.items;
            if (!Array.isArray(assortItems))
                continue;
            for (const assortItem of assortItems) {
                if (assortItem?.parentId !== "hideout" || assortItem?.slotId !== "hideout")
                    continue;
                if (this.applyWeaponDurability(assortItem, items, "trader")) {
                    changed++;
                }
            }
        }
        return changed;
    }
    applyLocationWeaponDurability(locations, items) {
        let changed = 0;
        for (const location of Object.values(locations ?? {})) {
            changed += this.applyWorldWeaponDurabilityToLootTemplates(location?.looseLoot, items);
        }
        return changed;
    }
    patchLocationLootGenerator(container, items, logger) {
        try {
            const locationLootGenerator = container.resolve("LocationLootGenerator");
            const prototype = Object.getPrototypeOf(locationLootGenerator);
            if (prototype.__breakContactWeaponDurabilityPatched)
                return;
            const mod = this;
            let patchedMethods = 0;
            for (const methodName of ["generateDynamicLoot", "generateStaticContainers"]) {
                const originalMethod = prototype[methodName];
                if (typeof originalMethod !== "function")
                    continue;
                prototype[methodName] = function (...args) {
                    const loot = originalMethod.apply(this, args);
                    if (typeof loot?.then === "function") {
                        return loot.then((resolvedLoot) => {
                            mod.applyWorldWeaponDurabilityToLootTemplates(resolvedLoot, items);
                            return resolvedLoot;
                        });
                    }
                    mod.applyWorldWeaponDurabilityToLootTemplates(loot, items);
                    return loot;
                };
                patchedMethods++;
            }
            if (patchedMethods === 0) {
                const warning = logger.warning ?? logger.error;
                warning.call(logger, "Break Contact could not find LocationLootGenerator methods to patch for per-raid weapon durability.");
            }
            Object.defineProperty(prototype, "__breakContactWeaponDurabilityPatched", { value: true });
        }
        catch (error) {
            const warning = logger.warning ?? logger.error;
            warning.call(logger, `Break Contact could not patch world weapon durability: ${error?.message ?? error}`);
        }
    }
    applyWorldWeaponDurabilityToLootTemplates(lootTemplates, items) {
        let changed = 0;
        if (!lootTemplates)
            return changed;
        if (Array.isArray(lootTemplates)) {
            for (const lootTemplate of lootTemplates) {
                changed += this.applyWorldWeaponDurabilityToLootTemplates(lootTemplate, items);
            }
            return changed;
        }
        if (typeof lootTemplates !== "object")
            return changed;
        changed += this.applyWorldWeaponDurabilityToItems(lootTemplates.Items, lootTemplates.Root, items);
        changed += this.applyWorldWeaponDurabilityToItems(lootTemplates.template?.Items, lootTemplates.template?.Root, items);
        changed += this.applyWorldWeaponDurabilityToLootTemplates(lootTemplates.spawnpoints, items);
        changed += this.applyWorldWeaponDurabilityToLootTemplates(lootTemplates.spawnpointsForced, items);
        changed += this.applyWorldWeaponDurabilityToLootTemplates(lootTemplates.looseLoot, items);
        return changed;
    }
    applyWorldWeaponDurabilityToItems(itemList, rootId, items) {
        let changed = 0;
        if (!Array.isArray(itemList))
            return changed;
        for (const item of itemList) {
            if (!this.isWeaponTemplate(item?._tpl, items))
                continue;
            if (item._id !== rootId && this.hasWeaponParent(item, itemList, items))
                continue;
            if (this.applyWeaponDurability(item, items, "world")) {
                changed++;
            }
        }
        return changed;
    }
    applyWeaponDurability(item, items, source) {
        const template = items[item?._tpl];
        if (!template || !this.isWeaponTemplate(item._tpl, items))
            return false;
        const templateMaxDurability = Number(template._props?.MaxDurability ?? template._props?.Durability ?? 0);
        if (templateMaxDurability <= 0)
            return false;
        const range = this.getDurabilityRange(item._tpl, source);
        const chosenPercent = this.randomInt(range.min, range.max);
        const chosenMaxDurability = Math.max(1, Math.min(templateMaxDurability, Math.round(templateMaxDurability * chosenPercent / 100)));
        item.upd = item.upd ?? {};
        item.upd.Repairable = {
            Durability: chosenMaxDurability,
            MaxDurability: chosenMaxDurability
        };
        return true;
    }
    getDurabilityRange(itemTpl, source) {
        const settings = (config.WeaponDurability ?? {});
        const value = this.weaponBaseValues[itemTpl] ?? this.weaponValueLow;
        const valueFactor = this.clamp((value - this.weaponValueLow) / (this.weaponValueHigh - this.weaponValueLow), 0, 1);
        const lowMin = source === "trader" ? settings.TraderLowValueMin : settings.WorldLowValueMin;
        const lowMax = source === "trader" ? settings.TraderLowValueMax : settings.WorldLowValueMax;
        const highMin = source === "trader" ? settings.TraderHighValueMin : settings.WorldHighValueMin;
        const highMax = source === "trader" ? settings.TraderHighValueMax : settings.WorldHighValueMax;
        const min = Math.round(this.lerp(this.clampPercent(lowMin, 35), this.clampPercent(highMin, 80), valueFactor));
        const max = Math.round(this.lerp(this.clampPercent(lowMax, 95), this.clampPercent(highMax, 98), valueFactor));
        return {
            min: Math.min(min, max),
            max: Math.max(min, max)
        };
    }
    hasWeaponParent(item, itemList, items) {
        let parent = itemList.find((candidate) => candidate?._id === item?.parentId);
        while (parent) {
            if (this.isWeaponTemplate(parent._tpl, items))
                return true;
            parent = itemList.find((candidate) => candidate?._id === parent.parentId);
        }
        return false;
    }
    isWeaponTemplate(id, items) {
        return id === this.weaponID || this.isChildOf(id, this.weaponID, items);
    }
    isChildOf(id, parentID, items) {
        if (!id || !items[id])
            return false;
        let current = items[id]._parent;
        while (current) {
            if (current === parentID)
                return true;
            current = items[current]?._parent;
        }
        return false;
    }
    percentile(sortedValues, percentile) {
        if (sortedValues.length === 0)
            return 0;
        const index = Math.round((sortedValues.length - 1) * percentile);
        return sortedValues[this.clamp(index, 0, sortedValues.length - 1)];
    }
    normalizePercentile(value, fallback) {
        if (typeof value !== "number" || !Number.isFinite(value))
            return fallback;
        return this.clamp(value > 1 ? value / 100 : value, 0, 1);
    }
    clampPercent(value, fallback) {
        if (typeof value !== "number" || !Number.isFinite(value))
            return fallback;
        return this.clamp(value, 0, 100);
    }
    clamp(value, min, max) {
        return Math.min(max, Math.max(min, value));
    }
    lerp(start, end, value) {
        return start + (end - start) * value;
    }
    randomInt(min, max) {
        return Math.floor(Math.random() * (max - min + 1)) + min;
    }
}
module.exports = { mod: new Mod() };
