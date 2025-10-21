/** Разделы проекта. */
export enum Pages {
  Problems,
  Tasks,
  Agents
}

/** Типы агентов. */
export enum AgentType {
  /** Источник входящих заявок. */
  Source,
  /** Блок приборов. */
  ServiceBlock,
  /** Очередь. */
  Buffer,
  /** Заявка. */
  Call,
  /** Орбита. */
  Orbit
}

export interface IAgent {
  /** Идентификатор агента. */
  readonly id: string;
  readonly type: AgentType;
  readonly properties: Record<string, any>;
}

export interface IProblem {
  readonly name: string;
  readonly agents?: IAgent[];
  readonly links?: ReadonlyMap<string, ReadonlyArray<string>>;
}
