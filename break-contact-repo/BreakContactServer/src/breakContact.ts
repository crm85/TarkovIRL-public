import { DependencyContainer } from "tsyringe";

import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { DatabaseServer } from "@spt/servers/DatabaseServer";
import * as config from "../config/config.json";

type FenceRollConfigSnapshot = {
    blacklist: any;
    blacklistSeasonalItems: any;
    ammoMaxPenLimit: any;
    itemTypeLimits: any;
    itemCategoryRoublePriceLimit: any;
};

class Mod implements IPreSptLoadMod, IPostDBLoadMod {
    private weaponBaseValues: Record<string, number> = {};
    private weaponValueLow = 0;
    private weaponValueHigh = 1;
    private readonly weaponID = "5422acb9af1c889c16000029";
    private readonly fenceTraderID = "579dc571d53a0658a154fbec";
    private readonly roublesID = "5449016a4bdc2d6f028b456f";
    private readonly ammoBoxID = "543be5cb4bdc2deb348b4568";
    private readonly expandedFenceInvalidBaseTypes = [
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

    public preSptLoad(container: DependencyContainer): void {
        const logger = container.resolve<ILogger>("WinstonLogger");

        this.patchExpandedFenceAssort(container, logger);
    }

    private patchExpandedFenceAssort(container: DependencyContainer, logger: ILogger): void {
        if (!this.getExpandedFenceSettings().enabled) return;

        container.afterResolution(
            "FenceBaseAssortGenerator",
            (_token: unknown, fenceBaseAssortGenerator: any) => {
                if (!fenceBaseAssortGenerator || fenceBaseAssortGenerator.__breakContactExpandedFenceBasePatched) return;

                const originalGenerateFenceBaseAssorts = fenceBaseAssortGenerator.generateFenceBaseAssorts;
                if (typeof originalGenerateFenceBaseAssorts !== "function") {
                    this.logWarning(logger, "Break Contact could not patch Fence base assort: generateFenceBaseAssorts was not found.");
                    return;
                }

                const mod = this;
                fenceBaseAssortGenerator.generateFenceBaseAssorts = function (...args: any[]) {
                    const result = originalGenerateFenceBaseAssorts.apply(this, args);
                    const addedCount = mod.expandFenceBaseAssort(this, logger);
                    if (mod.getExpandedFenceSettings().debugLogging) {
                        logger.info(`Break Contact added ${addedCount} extra item template(s) to Fence's roll pool.`);
                    }

                    return result;
                };

                fenceBaseAssortGenerator.__breakContactExpandedFenceBasePatched = true;
            },
            { frequency: "Always" }
        );

        container.afterResolution(
            "FenceService",
            (_token: unknown, fenceService: any) => {
                if (!fenceService || fenceService.__breakContactExpandedFenceRollPatched) return;

                const originalCreateAssorts = fenceService.createAssorts;
                if (typeof originalCreateAssorts !== "function") {
                    this.logWarning(logger, "Break Contact could not patch Fence assort rolls: createAssorts was not found.");
                    return;
                }

                const mod = this;
                fenceService.createAssorts = function (...args: any[]) {
                    if (!mod.getExpandedFenceSettings().enabled) {
                        return originalCreateAssorts.apply(this, args);
                    }

                    return mod.withExpandedFenceRollConfig(this?.traderConfig?.fence, () => originalCreateAssorts.apply(this, args));
                };

                fenceService.__breakContactExpandedFenceRollPatched = true;
            },
            { frequency: "Always" }
        );
    }

    private getExpandedFenceSettings(): { enabled: boolean; debugLogging: boolean } {
        const settings = ((config as any).ExpandedFenceAssort ?? {}) as Record<string, any>;

        return {
            enabled: settings.Enabled !== false,
            debugLogging: settings.DebugLogging === true
        };
    }

    private withExpandedFenceRollConfig<T>(fenceConfig: any, action: () => T): T {
        if (!fenceConfig) return action();

        const snapshot = this.captureFenceRollConfig(fenceConfig);
        fenceConfig.blacklist = [];
        fenceConfig.blacklistSeasonalItems = false;
        fenceConfig.ammoMaxPenLimit = Number.MAX_SAFE_INTEGER;
        fenceConfig.itemTypeLimits = {};
        fenceConfig.itemCategoryRoublePriceLimit = {};

        try {
            return action();
        } finally {
            this.restoreFenceRollConfig(fenceConfig, snapshot);
        }
    }

    private captureFenceRollConfig(fenceConfig: any): FenceRollConfigSnapshot {
        return {
            blacklist: fenceConfig.blacklist,
            blacklistSeasonalItems: fenceConfig.blacklistSeasonalItems,
            ammoMaxPenLimit: fenceConfig.ammoMaxPenLimit,
            itemTypeLimits: fenceConfig.itemTypeLimits,
            itemCategoryRoublePriceLimit: fenceConfig.itemCategoryRoublePriceLimit
        };
    }

    private restoreFenceRollConfig(fenceConfig: any, snapshot: FenceRollConfigSnapshot): void {
        fenceConfig.blacklist = snapshot.blacklist;
        fenceConfig.blacklistSeasonalItems = snapshot.blacklistSeasonalItems;
        fenceConfig.ammoMaxPenLimit = snapshot.ammoMaxPenLimit;
        fenceConfig.itemTypeLimits = snapshot.itemTypeLimits;
        fenceConfig.itemCategoryRoublePriceLimit = snapshot.itemCategoryRoublePriceLimit;
    }

    private expandFenceBaseAssort(fenceBaseAssortGenerator: any, logger: ILogger): number {
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

        const existingRootTpls = new Set<string>();
        for (const item of baseFenceAssort.items ?? []) {
            if (item?.parentId === "hideout" && typeof item?._tpl === "string") {
                existingRootTpls.add(item._tpl);
            }
        }

        let added = 0;
        for (const itemDb of items) {
            if (!this.shouldAddExpandedFenceItem(itemDb, itemHelper, existingRootTpls)) continue;

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
            if (!this.hasRequiredExpandedFenceChildren(itemDb, itemWithChildrenToAdd)) continue;

            itemHelper.remapRootItemId(itemWithChildrenToAdd);
            if (itemWithChildrenToAdd.length > 1) {
                itemHelper.reparentItemAndChildren(itemWithChildrenToAdd[0], itemWithChildrenToAdd);
                itemWithChildrenToAdd[0].parentId = "hideout";
                itemWithChildrenToAdd[0].slotId = "hideout";
            }

            let price = Math.round(this.getExpandedFenceItemPrice(fenceService, itemHelper, itemDb._id, itemWithChildrenToAdd));
            if (!Number.isFinite(price) || price <= 1) continue;
            if (price === 100) price = 101;

            const rootItem = itemWithChildrenToAdd[0];
            baseFenceAssort.barter_scheme[rootItem._id] = [[{ count: price, _tpl: this.roublesID }]];
            baseFenceAssort.items.push(...itemWithChildrenToAdd);
            baseFenceAssort.loyal_level_items[rootItem._id] = 1;
            existingRootTpls.add(itemDb._id);
            added++;
        }

        return added;
    }

    private shouldAddExpandedFenceItem(itemDb: any, itemHelper: any, existingRootTpls: Set<string>): boolean {
        if (!itemDb || typeof itemDb._id !== "string" || existingRootTpls.has(itemDb._id)) return false;
        if (itemDb._type !== "Item") return false;
        if (itemDb._props?.QuestItem === true) return false;

        const prefabPath = itemDb._props?.Prefab?.path;
        if (typeof prefabPath !== "string" || prefabPath.length === 0) return false;

        if (typeof itemHelper.isValidItem === "function") {
            return itemHelper.isValidItem(itemDb._id, this.expandedFenceInvalidBaseTypes);
        }

        return true;
    }

    private addExpandedFenceItemChildren(fenceBaseAssortGenerator: any, itemWithChildren: any[], itemDb: any): void {
        const itemHelper = fenceBaseAssortGenerator?.itemHelper;
        if (!itemHelper) return;

        if (this.isItemOfBaseclass(itemHelper, itemDb._id, this.ammoBoxID)) {
            itemHelper.addCartridgesToAmmoBox?.(itemWithChildren, itemDb);
        }

        if (!Array.isArray(itemDb?._props?.Slots) || itemDb._props.Slots.length === 0) return;

        if (typeof itemHelper.armorItemCanHoldMods === "function" && itemHelper.armorItemCanHoldMods(itemDb._id)) {
            fenceBaseAssortGenerator.addChildrenToArmorModSlots?.(itemWithChildren, itemDb);
        }
    }

    private hasRequiredExpandedFenceChildren(itemDb: any, itemWithChildren: any[]): boolean {
        const requiredSlots = Array.isArray(itemDb?._props?.Slots)
            ? itemDb._props.Slots.filter((slot: any) => slot?._required === true && typeof slot?._name === "string")
            : [];

        if (requiredSlots.length === 0) return true;

        const rootId = itemWithChildren[0]?._id;
        return requiredSlots.every((slot: any) => itemWithChildren.some((item) => item?.parentId === rootId && item?.slotId === slot._name));
    }

    private getExpandedFenceItemPrice(fenceService: any, itemHelper: any, itemTpl: string, itemWithChildren: any[]): number {
        const fencePrice = Number(fenceService?.getItemPrice?.(itemTpl, itemWithChildren) ?? 0);
        if (Number.isFinite(fencePrice) && fencePrice > 0) return fencePrice;

        return Number(itemHelper?.getItemPrice?.(itemTpl) ?? 0);
    }

    private isItemOfBaseclass(itemHelper: any, itemTpl: string, baseClass: string): boolean {
        return typeof itemHelper?.isOfBaseclass === "function" && itemHelper.isOfBaseclass(itemTpl, baseClass);
    }

    public postDBLoad(container: DependencyContainer): void {
        const logger = container.resolve<ILogger>("WinstonLogger");
        const db = container.resolve<DatabaseServer>("DatabaseServer");
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

    private logWarning(logger: ILogger, message: string): void {
        const warning = (logger as any).warning ?? logger.error;
        warning.call(logger, message);
    }

    private configureWeaponDurability(items: Record<string, any>, handbook: any): void {
        this.weaponBaseValues = {};

        for (const handbookItem of handbook?.Items ?? []) {
            if (typeof handbookItem?.Id === "string" && typeof handbookItem?.Price === "number") {
                this.weaponBaseValues[handbookItem.Id] = handbookItem.Price;
            }
        }

        const weaponValues: number[] = [];
        for (const id in items) {
            if (!this.isWeaponTemplate(id, items)) continue;

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
        this.weaponValueLow = this.percentile(
            weaponValues,
            this.normalizePercentile(config.WeaponDurability?.ValuePercentileLow, 0.1)
        );
        this.weaponValueHigh = this.percentile(
            weaponValues,
            this.normalizePercentile(config.WeaponDurability?.ValuePercentileHigh, 0.9)
        );

        if (this.weaponValueHigh <= this.weaponValueLow) {
            this.weaponValueHigh = this.weaponValueLow + 1;
        }
    }

    private applyTraderWeaponDurability(traders: Record<string, any>, items: Record<string, any>): number {
        let changed = 0;

        for (const traderId in traders) {
            const assortItems = traders[traderId]?.assort?.items;
            if (!Array.isArray(assortItems)) continue;

            for (const assortItem of assortItems) {
                if (assortItem?.parentId !== "hideout" || assortItem?.slotId !== "hideout") continue;

                if (this.applyWeaponDurability(assortItem, items, "trader")) {
                    changed++;
                }
            }
        }

        return changed;
    }

    private applyLocationWeaponDurability(locations: Record<string, any>, items: Record<string, any>): number {
        let changed = 0;

        for (const location of Object.values(locations ?? {})) {
            changed += this.applyWorldWeaponDurabilityToLootTemplates(location?.looseLoot, items);
        }

        return changed;
    }

    private patchLocationLootGenerator(container: DependencyContainer, items: Record<string, any>, logger: ILogger): void {
        try {
            const locationLootGenerator = container.resolve<any>("LocationLootGenerator");
            const prototype = Object.getPrototypeOf(locationLootGenerator);
            if (prototype.__breakContactWeaponDurabilityPatched) return;

            const mod = this;
            let patchedMethods = 0;

            for (const methodName of ["generateDynamicLoot", "generateStaticContainers"]) {
                const originalMethod = prototype[methodName];
                if (typeof originalMethod !== "function") continue;

                prototype[methodName] = function (...args: any[]) {
                    const loot = originalMethod.apply(this, args);
                    if (typeof loot?.then === "function") {
                        return loot.then((resolvedLoot: any) => {
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
                const warning = (logger as any).warning ?? (logger as any).error;
                warning.call(logger, "Break Contact could not find LocationLootGenerator methods to patch for per-raid weapon durability.");
            }

            Object.defineProperty(prototype, "__breakContactWeaponDurabilityPatched", { value: true });
        } catch (error) {
            const warning = (logger as any).warning ?? (logger as any).error;
            warning.call(logger, `Break Contact could not patch world weapon durability: ${error?.message ?? error}`);
        }
    }

    private applyWorldWeaponDurabilityToLootTemplates(lootTemplates: any, items: Record<string, any>): number {
        let changed = 0;
        if (!lootTemplates) return changed;

        if (Array.isArray(lootTemplates)) {
            for (const lootTemplate of lootTemplates) {
                changed += this.applyWorldWeaponDurabilityToLootTemplates(lootTemplate, items);
            }

            return changed;
        }

        if (typeof lootTemplates !== "object") return changed;

        changed += this.applyWorldWeaponDurabilityToItems(lootTemplates.Items, lootTemplates.Root, items);
        changed += this.applyWorldWeaponDurabilityToItems(lootTemplates.template?.Items, lootTemplates.template?.Root, items);
        changed += this.applyWorldWeaponDurabilityToLootTemplates(lootTemplates.spawnpoints, items);
        changed += this.applyWorldWeaponDurabilityToLootTemplates(lootTemplates.spawnpointsForced, items);
        changed += this.applyWorldWeaponDurabilityToLootTemplates(lootTemplates.looseLoot, items);

        return changed;
    }

    private applyWorldWeaponDurabilityToItems(itemList: any[], rootId: string | undefined, items: Record<string, any>): number {
        let changed = 0;
        if (!Array.isArray(itemList)) return changed;

        for (const item of itemList) {
            if (!this.isWeaponTemplate(item?._tpl, items)) continue;
            if (item._id !== rootId && this.hasWeaponParent(item, itemList, items)) continue;

            if (this.applyWeaponDurability(item, items, "world")) {
                changed++;
            }
        }

        return changed;
    }

    private applyWeaponDurability(item: any, items: Record<string, any>, source: "world" | "trader"): boolean {
        const template = items[item?._tpl];
        if (!template || !this.isWeaponTemplate(item._tpl, items)) return false;

        const templateMaxDurability = Number(template._props?.MaxDurability ?? template._props?.Durability ?? 0);
        if (templateMaxDurability <= 0) return false;

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

    private getDurabilityRange(itemTpl: string, source: "world" | "trader"): { min: number; max: number } {
        const settings = (config.WeaponDurability ?? {}) as Record<string, number>;
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

    private hasWeaponParent(item: any, itemList: any[], items: Record<string, any>): boolean {
        let parent = itemList.find((candidate) => candidate?._id === item?.parentId);
        while (parent) {
            if (this.isWeaponTemplate(parent._tpl, items)) return true;
            parent = itemList.find((candidate) => candidate?._id === parent.parentId);
        }

        return false;
    }

    private isWeaponTemplate(id: string, items: Record<string, any>): boolean {
        return id === this.weaponID || this.isChildOf(id, this.weaponID, items);
    }

    private isChildOf(id: string, parentID: string, items: Record<string, any>): boolean {
        if (!id || !items[id]) return false;

        let current = items[id]._parent;
        while (current) {
            if (current === parentID) return true;
            current = items[current]?._parent;
        }

        return false;
    }

    private percentile(sortedValues: number[], percentile: number): number {
        if (sortedValues.length === 0) return 0;

        const index = Math.round((sortedValues.length - 1) * percentile);
        return sortedValues[this.clamp(index, 0, sortedValues.length - 1)];
    }

    private normalizePercentile(value: number | undefined, fallback: number): number {
        if (typeof value !== "number" || !Number.isFinite(value)) return fallback;

        return this.clamp(value > 1 ? value / 100 : value, 0, 1);
    }

    private clampPercent(value: number | undefined, fallback: number): number {
        if (typeof value !== "number" || !Number.isFinite(value)) return fallback;

        return this.clamp(value, 0, 100);
    }

    private clamp(value: number, min: number, max: number): number {
        return Math.min(max, Math.max(min, value));
    }

    private lerp(start: number, end: number, value: number): number {
        return start + (end - start) * value;
    }

    private randomInt(min: number, max: number): number {
        return Math.floor(Math.random() * (max - min + 1)) + min;
    }
}

module.exports = { mod: new Mod() };
