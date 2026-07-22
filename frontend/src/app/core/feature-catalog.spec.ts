import { describe, expect, it } from 'vitest';
import { featureCatalog } from './feature-catalog.generated';

describe('generated feature catalog', () => {
  it('keeps every detailed design and L2 operation traceable', () => {
    expect(featureCatalog).toHaveLength(82);
    expect(featureCatalog.reduce((count, feature) => count + feature.operations.length, 0)).toBe(
      260,
    );
    expect(new Set(featureCatalog.map((feature) => feature.slug)).size).toBe(82);
    expect(
      new Set(
        featureCatalog.flatMap((feature) => feature.operations.map((item) => item.requirementId)),
      ).size,
    ).toBe(260);
  });
});
