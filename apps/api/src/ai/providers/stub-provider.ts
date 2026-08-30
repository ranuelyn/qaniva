import type {
  AiProvider,
  DebriefContext,
  DebriefNarrative,
  PatientReply,
  PatientTurnContext,
} from '../ai.types';

/**
 * Deterministic, offline provider. It NEVER invents clinical content: the patient
 * reply is assembled only from already-disclosed facts, and the debrief narrative
 * is a template over the deterministic timeline. This is also the fallback the
 * gateway uses when a real provider times out or returns an invalid response.
 */
export class StubAiProvider implements AiProvider {
  readonly name = 'stub';

  async patientReply(ctx: PatientTurnContext): Promise<PatientReply> {
    const question = ctx.userMessage.toLowerCase();
    const realAdviceAsk = /should i|what.*wrong with me|will i (die|be ok)|diagnos/.test(question);
    if (realAdviceAsk) {
      return {
        reply:
          "I can't answer that — this is a training simulation. Ask me about how I feel or my history.",
        usedFactIds: [],
        outOfScope: true,
      };
    }

    const relevant = ctx.disclosedFacts.filter((f) =>
      question.split(/\W+/).some((w) => w.length > 3 && f.text.toLowerCase().includes(w)),
    );
    const chosen = relevant.length > 0 ? relevant : ctx.disclosedFacts.slice(0, 1);

    if (chosen.length === 0) {
      return { reply: "I'm not sure how to answer that.", usedFactIds: [], outOfScope: false };
    }

    return {
      reply: chosen.map((f) => f.text).join(' '),
      usedFactIds: chosen.map((f) => f.id),
      outOfScope: false,
    };
  }

  async debriefNarrative(ctx: DebriefContext): Promise<DebriefNarrative> {
    const correct = ctx.timeline.filter((e) => e.classification === 'correct').length;
    const delayed = ctx.timeline.filter((e) => e.classification === 'delayed').length;
    const harmful = ctx.timeline.filter((e) => e.classification === 'harmful').length;

    const parts = [
      `You completed ${ctx.timeline.length} actions and scored ${ctx.totalScore}.`,
      `${correct} were on target${delayed ? `, ${delayed} were late` : ''}${
        harmful ? `, and ${harmful} were harmful` : ''
      }.`,
    ];
    if (ctx.missedCriterionIds.length > 0) {
      parts.push(`Missed objectives: ${ctx.missedCriterionIds.join(', ')}.`);
    }
    for (const note of ctx.approvedEvidenceNotes) {
      parts.push(note);
    }

    return { narrative: parts.join(' '), reportedScore: ctx.totalScore };
  }
}
