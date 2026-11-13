import { z } from "zod";

export const vec3Schema = z.object({
  x: z.number().finite().min(-100_000).max(100_000),
  y: z.number().finite().min(-100_000).max(100_000),
  z: z.number().finite().min(-100_000).max(100_000)
});

export const createSphereSchema = z.object({
  diameterMm: z.number().finite().positive().max(10_000),
  center: vec3Schema.optional()
});

export const createBoxSchema = z.object({
  widthMm: z.number().finite().positive().max(10_000),
  depthMm: z.number().finite().positive().max(10_000),
  heightMm: z.number().finite().positive().max(10_000),
  center: vec3Schema.optional()
});

export const createCylinderSchema = z.object({
  radiusMm: z.number().finite().positive().max(5_000),
  heightMm: z.number().finite().positive().max(10_000),
  topRadiusMm: z.number().finite().min(0).max(5_000).optional(),
  center: vec3Schema.optional()
});

export const moveObjectSchema = z.object({
  objectId: z.string().min(1).max(128),
  translation: vec3Schema,
  mode: z.enum(["relative", "absolute"]).default("relative")
});

export const deleteObjectSchema = z.object({
  objectId: z.string().min(1).max(128),
  confirm: z.literal(true)
});

export const listObjectsSchema = z.object({
  includeTransforms: z.boolean().default(true)
});

export type Vec3 = z.infer<typeof vec3Schema>;
export type SceneCommandName =
  | "create_sphere"
  | "create_box"
  | "create_cylinder"
  | "move_object"
  | "delete_object"
  | "list_objects";

export interface SceneObjectRecord {
  id: string;
  type: "sphere" | "box" | "cylinder";
  name: string;
  dimensionsMm: Record<string, number>;
  positionMm: Vec3;
}

export interface SceneCommand {
  commandId: string;
  operation: SceneCommandName;
  payload: unknown;
}

export interface SceneCommandAck {
  commandId: string;
  success: boolean;
  result?: unknown;
  error?: string;
  sceneRevision: number;
}
