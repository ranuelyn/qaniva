import Ajv2020, { type ErrorObject } from 'ajv/dist/2020';
import addFormatsImport from 'ajv-formats';
import schemaJson from '../schema/case.schema.json';

// ajv-formats is CJS (`module.exports = formatsPlugin`); normalise the default.
const addFormats = addFormatsImport as unknown as (ajv: unknown) => unknown;

export const caseJsonSchema = schemaJson as Record<string, unknown>;

const ajv = new Ajv2020({ allErrors: true, strict: false });
addFormats(ajv);
const validateFn = ajv.compile(caseJsonSchema);

export interface ValidationIssue {
  path: string;
  message: string;
}

export interface ValidationResult {
  valid: boolean;
  issues: ValidationIssue[];
}

function fmtAjvError(e: ErrorObject): ValidationIssue {
  return { path: e.instancePath || '(root)', message: `${e.message ?? 'invalid'}` };
}

/**
 * Structural (JSON Schema) validation only.
 */
export function validateCaseStructure(data: unknown): ValidationResult {
  const valid = validateFn(data);
  if (valid) return { valid: true, issues: [] };
  return {
    valid: false,
    issues: (validateFn.errors ?? []).map(fmtAjvError),
  };
}

// --- Semantic / cross-reference validation --------------------------------

interface CaseShape {
  availableActions: {
    id: string;
    criterionIds: string[];
    visibility: string;
    visibleWhen: unknown;
  }[];
  transitionRules: { id: string; terminalState: string | null }[];
  scoringCriteria: { id: string; acceptedActions: string[] }[];
  terminalStates: { id: string }[];
  hiddenFacts: { id: string }[];
}

/**
 * Checks that identifiers referenced across sections actually resolve.
 * Runs only after structure validation passes.
 */
export function validateCaseSemantics(data: unknown): ValidationResult {
  const c = data as CaseShape;
  const issues: ValidationIssue[] = [];

  const actionIds = new Set(c.availableActions.map((a) => a.id));
  const criterionIds = new Set(c.scoringCriteria.map((s) => s.id));
  const terminalIds = new Set(c.terminalStates.map((t) => t.id));

  const dupActions = findDuplicates(c.availableActions.map((a) => a.id));
  if (dupActions.length)
    issues.push({
      path: 'availableActions',
      message: `duplicate action ids: ${dupActions.join(', ')}`,
    });

  const dupCriteria = findDuplicates(c.scoringCriteria.map((s) => s.id));
  if (dupCriteria.length)
    issues.push({
      path: 'scoringCriteria',
      message: `duplicate criterion ids: ${dupCriteria.join(', ')}`,
    });

  for (const a of c.availableActions) {
    if (a.visibility === 'when' && !a.visibleWhen) {
      issues.push({
        path: `availableActions/${a.id}`,
        message: 'visibility="when" requires a non-null visibleWhen expression',
      });
    }
    for (const cid of a.criterionIds) {
      if (!criterionIds.has(cid)) {
        issues.push({
          path: `availableActions/${a.id}/criterionIds`,
          message: `unknown criterion id "${cid}"`,
        });
      }
    }
  }

  for (const s of c.scoringCriteria) {
    for (const aid of s.acceptedActions) {
      if (!actionIds.has(aid)) {
        issues.push({
          path: `scoringCriteria/${s.id}/acceptedActions`,
          message: `unknown action id "${aid}"`,
        });
      }
    }
  }

  for (const r of c.transitionRules) {
    if (r.terminalState && !terminalIds.has(r.terminalState)) {
      issues.push({
        path: `transitionRules/${r.id}/terminalState`,
        message: `unknown terminal state "${r.terminalState}"`,
      });
    }
  }

  const dupFacts = findDuplicates(c.hiddenFacts.map((f) => f.id));
  if (dupFacts.length)
    issues.push({ path: 'hiddenFacts', message: `duplicate fact ids: ${dupFacts.join(', ')}` });

  return { valid: issues.length === 0, issues };
}

/** Full validation: structure then semantics. */
export function validateCase(data: unknown): ValidationResult {
  const structural = validateCaseStructure(data);
  if (!structural.valid) return structural;
  return validateCaseSemantics(data);
}

function findDuplicates(values: string[]): string[] {
  const seen = new Set<string>();
  const dups = new Set<string>();
  for (const v of values) {
    if (seen.has(v)) dups.add(v);
    seen.add(v);
  }
  return [...dups];
}
