import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import type { z } from 'zod';
import {
  startSimulationMessageSchema,
  exitSimulationMessageSchema,
  simulationReadyMessageSchema,
  simulationCompletedMessageSchema,
  simulationFailedMessageSchema,
  exitRequestedMessageSchema,
  attemptSummarySchema,
  scoreBreakdownSchema,
  timelineEntrySchema,
  criterionResultSchema,
  debriefContentSchema,
} from '../index';

/**
 * STRUCTURAL drift guard between the Zod contracts (source of truth) and the C#
 * mirror DTOs in unity/.../Bridge/BridgeEnvelope.cs.
 *
 * Strategy (see docs/architecture/rn-unity-boundary.md): rather than maintaining a
 * second hand-written description or building a code generator, this test
 * INTROSPECTS the Zod schemas at runtime and PARSES the C# public-field
 * declarations, then compares, per payload class:
 *   - field presence in both directions (no missing / no extra fields),
 *   - primitive category (string / number / boolean / array / object),
 *   - nested object structure, recursively (AttemptSummary -> breakdown/timeline),
 *   - that TS enum/datetime/uuid fields are C# strings,
 *   - that required TS numbers/booleans are C# value types (cannot be silently null).
 *
 * The companion test (csharp-parity.test.ts) covers the protocol version and the
 * message-type / failure-code string constants.
 */

const CSHARP_ENVELOPE = resolve(
  __dirname,
  '../../../../unity/QanivaSimulation/Assets/Qaniva/Scripts/Bridge/BridgeEnvelope.cs',
);

// --- C# side: parse public field declarations per class -------------------

type CsCategory = 'string' | 'number' | 'boolean' | 'array' | 'object';

interface CsField {
  name: string;
  rawType: string;
  category: CsCategory;
  /** element/nested class name for arrays and objects */
  nested?: string;
  /** value types cannot be null in C# — used to check required primitives */
  isValueType: boolean;
}

const CS_NUMBER_TYPES = new Set(['int', 'long', 'double', 'float', 'short', 'byte', 'decimal']);

function categorize(rawType: string): Pick<CsField, 'category' | 'nested' | 'isValueType'> {
  const listMatch = rawType.match(/^List<(.+)>$/);
  if (listMatch) return { category: 'array', nested: listMatch[1]!, isValueType: false };
  if (rawType === 'string') return { category: 'string', isValueType: false };
  if (CS_NUMBER_TYPES.has(rawType)) return { category: 'number', isValueType: true };
  if (rawType === 'bool') return { category: 'boolean', isValueType: true };
  return { category: 'object', nested: rawType, isValueType: false };
}

function parseCsClasses(source: string): Map<string, Map<string, CsField>> {
  const classes = new Map<string, Map<string, CsField>>();
  const classRegex = /class\s+(\w+)\s*\{([\s\S]*?)\n {4}\}/g;
  for (const match of source.matchAll(classRegex)) {
    const [, className, body] = match;
    const fields = new Map<string, CsField>();
    const fieldRegex = /public\s+([\w<>.]+)\s+(\w+)\s*(?:=[^;]+)?;/g;
    for (const fm of body!.matchAll(fieldRegex)) {
      const [, rawType, name] = fm;
      fields.set(name!, { name: name!, rawType: rawType!, ...categorize(rawType!) });
    }
    classes.set(className!, fields);
  }
  return classes;
}

// --- TS side: introspect Zod shapes -----------------------------------

interface TsField {
  name: string;
  category: CsCategory;
  optional: boolean;
  /** for objects/arrays-of-objects: the Zod object schema to recurse into */
  nestedShape?: z.ZodRawShape;
}

function unwrap(schema: z.ZodTypeAny): { inner: z.ZodTypeAny; optional: boolean } {
  let s = schema;
  let optional = false;
  // Unwrap optional/nullable/default wrappers.
  for (;;) {
    const def = s._def as { typeName: string };
    if (def.typeName === 'ZodOptional' || def.typeName === 'ZodNullable') {
      optional = true;
      s = (s._def as { innerType: z.ZodTypeAny }).innerType;
    } else if (def.typeName === 'ZodDefault') {
      s = (s._def as { innerType: z.ZodTypeAny }).innerType;
    } else {
      return { inner: s, optional };
    }
  }
}

function tsFieldOf(name: string, schema: z.ZodTypeAny): TsField {
  const { inner, optional } = unwrap(schema);
  const typeName = (inner._def as { typeName: string }).typeName;
  switch (typeName) {
    case 'ZodString':
      return { name, category: 'string', optional };
    case 'ZodNumber':
      return { name, category: 'number', optional };
    case 'ZodBoolean':
      return { name, category: 'boolean', optional };
    case 'ZodEnum':
    case 'ZodLiteral':
      // enums & string literals travel as strings on the bridge
      return { name, category: 'string', optional };
    case 'ZodArray': {
      const element = unwrap((inner._def as { type: z.ZodTypeAny }).type).inner;
      const elementTypeName = (element._def as { typeName: string }).typeName;
      return {
        name,
        category: 'array',
        optional,
        nestedShape:
          elementTypeName === 'ZodObject' ? (element as z.AnyZodObject).shape : undefined,
      };
    }
    case 'ZodObject':
      return { name, category: 'object', optional, nestedShape: (inner as z.AnyZodObject).shape };
    default:
      throw new Error(
        `Unhandled Zod type "${typeName}" for field "${name}" — extend the parity test.`,
      );
  }
}

function shapeFields(shape: z.ZodRawShape): TsField[] {
  return Object.entries(shape).map(([name, schema]) => tsFieldOf(name, schema as z.ZodTypeAny));
}

// --- Comparison -----------------------------------------------------

const source = readFileSync(CSHARP_ENVELOPE, 'utf8');
const csClasses = parseCsClasses(source);

/**
 * Nested TS object shapes -> the C# class expected to mirror them. Registered so
 * recursion knows which class to descend into.
 */
const NESTED_CLASS_FOR: Array<{ shape: z.ZodRawShape; csClass: string }> = [
  { shape: attemptSummarySchema.shape, csClass: 'AttemptSummaryDto' },
  { shape: scoreBreakdownSchema.shape, csClass: 'ScoreBreakdownDto' },
  { shape: timelineEntrySchema.shape, csClass: 'TimelineEntryDto' },
  { shape: criterionResultSchema.shape, csClass: 'CriterionResultDto' },
  { shape: debriefContentSchema.shape, csClass: 'DebriefContentDto' },
];

function csClassForShape(shape: z.ZodRawShape): string | undefined {
  return NESTED_CLASS_FOR.find((e) => e.shape === shape)?.csClass;
}

function compareShape(csClassName: string, shape: z.ZodRawShape, path: string, errors: string[]) {
  const csFields = csClasses.get(csClassName);
  if (!csFields) {
    errors.push(`${path}: C# class "${csClassName}" not found in BridgeEnvelope.cs`);
    return;
  }
  const tsFields = shapeFields(shape);
  const tsNames = new Set(tsFields.map((f) => f.name));

  for (const ts of tsFields) {
    const cs = csFields.get(ts.name);
    if (!cs) {
      errors.push(`${path}.${ts.name}: missing from C# ${csClassName}`);
      continue;
    }
    if (cs.category !== ts.category) {
      errors.push(
        `${path}.${ts.name}: TS is ${ts.category} but C# ${csClassName}.${cs.name} is ${cs.category} (${cs.rawType})`,
      );
      continue;
    }
    // Note: required-ness of C# strings/objects cannot be read from the field
    // declaration (reference types); it is enforced by the runtime codecs. The
    // dedicated value-type test below covers required numbers/booleans.
    if (ts.nestedShape) {
      const nestedClass = csClassForShape(ts.nestedShape);
      if (!nestedClass) {
        errors.push(
          `${path}.${ts.name}: nested object shape has no registered C# mirror — register it in NESTED_CLASS_FOR`,
        );
      } else {
        if (cs.nested !== nestedClass) {
          errors.push(
            `${path}.${ts.name}: expected C# type ${nestedClass} but found ${cs.nested ?? cs.rawType}`,
          );
        }
        compareShape(nestedClass, ts.nestedShape, `${path}.${ts.name}`, errors);
      }
    }
  }

  for (const [name] of csFields) {
    if (!tsNames.has(name)) {
      errors.push(`${path}: C# ${csClassName}.${name} has no counterpart in the TS schema`);
    }
  }
}

function payloadShape(messageSchema: z.AnyZodObject): z.ZodRawShape {
  const payload = messageSchema.shape.payload as z.AnyZodObject;
  return payload.shape;
}

const MESSAGE_TO_CLASS: Array<{ label: string; schema: z.AnyZodObject; csClass: string }> = [
  {
    label: 'START_SIMULATION',
    schema: startSimulationMessageSchema,
    csClass: 'StartSimulationPayload',
  },
  {
    label: 'EXIT_SIMULATION',
    schema: exitSimulationMessageSchema,
    csClass: 'ExitSimulationPayload',
  },
  {
    label: 'SIMULATION_READY',
    schema: simulationReadyMessageSchema,
    csClass: 'SimulationReadyPayload',
  },
  {
    label: 'SIMULATION_COMPLETED',
    schema: simulationCompletedMessageSchema,
    csClass: 'SimulationCompletedPayload',
  },
  {
    label: 'SIMULATION_FAILED',
    schema: simulationFailedMessageSchema,
    csClass: 'SimulationFailedPayload',
  },
  { label: 'EXIT_REQUESTED', schema: exitRequestedMessageSchema, csClass: 'ExitRequestedPayload' },
];

describe('C# structural parity (payload shapes)', () => {
  it('parses the C# mirror classes', () => {
    // Sanity: if the C# file layout changes so the parser finds nothing, fail loudly
    // rather than passing vacuously.
    expect(csClasses.size).toBeGreaterThanOrEqual(9);
    expect(csClasses.get('AttemptSummaryDto')?.size).toBeGreaterThanOrEqual(10);
  });

  for (const { label, schema, csClass } of MESSAGE_TO_CLASS) {
    it(`${label} payload mirrors C# ${csClass}`, () => {
      const errors: string[] = [];
      compareShape(csClass, payloadShape(schema), label, errors);
      expect(errors).toEqual([]);
    });
  }

  it('the shared envelope fields exist on BridgeEnvelope', () => {
    const env = csClasses.get('BridgeEnvelope');
    expect(env).toBeDefined();
    for (const field of ['protocolVersion', 'type', 'messageId', 'sentAt']) {
      expect(env!.has(field), `BridgeEnvelope.${field} missing`).toBe(true);
    }
    expect(env!.get('protocolVersion')!.category).toBe('number');
  });

  it('required TS numbers/booleans are C# value types (cannot be silently null)', () => {
    const errors: string[] = [];
    for (const { label, schema, csClass } of MESSAGE_TO_CLASS) {
      const csFields = csClasses.get(csClass)!;
      for (const ts of shapeFields(payloadShape(schema))) {
        if (ts.optional || (ts.category !== 'number' && ts.category !== 'boolean')) continue;
        const cs = csFields.get(ts.name);
        if (cs && !cs.isValueType) {
          errors.push(`${label}.${ts.name}: required ${ts.category} is not a C# value type`);
        }
      }
    }
    expect(errors).toEqual([]);
  });
});
