import { test } from "node:test";
import assert from "node:assert/strict";
import { createSphereSchema, createBoxSchema, createCylinderSchema, moveObjectSchema } from "./index.ts";

test("sphere accepts positive diameter and rejects zero", () => {
  assert.equal(createSphereSchema.safeParse({ diameterMm: 20 }).success, true);
  assert.equal(createSphereSchema.safeParse({ diameterMm: 0 }).success, false);
});

test("box requires three positive dimensions", () => {
  assert.equal(createBoxSchema.safeParse({ widthMm: 10, depthMm: 10, heightMm: 10 }).success, true);
  assert.equal(createBoxSchema.safeParse({ widthMm: 10, depthMm: 0, heightMm: 10 }).success, false);
});

test("cylinder validates radius and height", () => {
  assert.equal(createCylinderSchema.safeParse({ radiusMm: 5, heightMm: 20 }).success, true);
  assert.equal(createCylinderSchema.safeParse({ radiusMm: -1, heightMm: 20 }).success, false);
});

test("move rejects non-finite coordinates", () => {
  assert.equal(moveObjectSchema.safeParse({ objectId: "obj_1", translation: { x: 50, y: 0, z: 0 } }).success, true);
  assert.equal(moveObjectSchema.safeParse({ objectId: "obj_1", translation: { x: "NaN", y: 0, z: 0 } }).success, false);
});

test("move accepts exact object name or type selector", () => {
  assert.equal(moveObjectSchema.safeParse({ objectName: "Sphere 1", translation: { x: 50, y: 0, z: 0 } }).success, true);
  assert.equal(moveObjectSchema.safeParse({ objectType: "sphere", translation: { x: 50, y: 0, z: 0 } }).success, true);
  assert.equal(moveObjectSchema.safeParse({ translation: { x: 50, y: 0, z: 0 } }).success, false);
  assert.equal(moveObjectSchema.safeParse({ objectId: "obj_1", objectName: "Sphere 1", translation: { x: 50, y: 0, z: 0 } }).success, false);
});
