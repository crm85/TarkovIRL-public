declare module "tsyringe" {
    export interface DependencyContainer {
        resolve<T>(token: string): T;
        afterResolution<T>(
            token: string,
            callback: (token: unknown, result: T) => void,
            options?: { frequency?: "Always" | "Once" }
        ): void;
    }
}

declare module "@spt/models/external/IPostDBLoadMod" {
    export interface IPostDBLoadMod {
        postDBLoad(container: unknown): void;
    }
}

declare module "@spt/models/external/IPreSptLoadMod" {
    export interface IPreSptLoadMod {
        preSptLoad(container: unknown): void;
    }
}

declare module "@spt/models/spt/utils/ILogger" {
    export interface ILogger {
        info(message: string): void;
        success(message: string): void;
        error(message: string): void;
        warning?(message: string): void;
    }
}

declare module "@spt/models/eft/profile/IUserDialogInfo" {
    export interface IUserDialogInfo {
        _id: string;
        aid?: number;
        Info: {
            Nickname: string;
            Side?: string;
            Level?: number;
            MemberCategory?: number;
            SelectedMemberCategory?: number;
        };
    }
}

declare module "@spt/models/enums/MemberCategory" {
    export enum MemberCategory {
        SYSTEM = 0
    }
}

declare module "@spt/models/enums/MessageType" {
    export enum MessageType {
        USER_MESSAGE = 1,
        MESSAGE_WITH_ITEMS = 4
    }
}

declare module "@spt/models/spt/dialog/ISendMessageDetails" {
    import type { IUserDialogInfo } from "@spt/models/eft/profile/IUserDialogInfo";

    export interface ISendMessageDetails {
        recipientId: string;
        sender: number;
        dialogType?: number;
        senderDetails?: IUserDialogInfo;
        messageText?: string;
        items?: any[];
        itemsMaxStorageLifetimeSeconds?: number;
    }
}

declare module "@spt/servers/DatabaseServer" {
    export interface DatabaseServer {
        getTables(): any;
    }
}

declare module "@spt/services/MailSendService" {
    import type { ISendMessageDetails } from "@spt/models/spt/dialog/ISendMessageDetails";

    export interface MailSendService {
        sendMessageToPlayer(details: ISendMessageDetails): void;
    }
}

declare module "@spt/services/mod/staticRouter/StaticRouterModService" {
    export interface StaticRoute {
        url: string;
        action: (url: string, info: any, sessionID: string, output: string) => any;
    }

    export interface StaticRouterModService {
        registerStaticRouter(name: string, routes: StaticRoute[], topLevelRoute?: string): void;
    }
}

declare module "@spt/utils/HttpResponseUtil" {
    export interface HttpResponseUtil {
        emptyResponse(): string;
    }
}

declare const module: {
    exports: any;
};
