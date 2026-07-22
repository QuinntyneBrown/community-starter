import { ChangeDetectionStrategy, Component } from '@angular/core';
import { featureCatalog } from '../../core/feature-catalog.generated';

interface CapabilityGroup {
  readonly subsystem: string;
  readonly label: string;
  readonly features: typeof featureCatalog;
  readonly operationCount: number;
}

@Component({
  selector: 'cs-catalog-page',
  templateUrl: './catalog.page.html',
  styleUrl: './capabilities.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogPage {
  protected readonly featureCount = featureCatalog.length;
  protected readonly operationCount = featureCatalog.reduce(
    (count, feature) => count + feature.operations.length,
    0,
  );
  protected readonly groups = buildGroups();
}

function buildGroups(): readonly CapabilityGroup[] {
  const subsystems = [...new Set(featureCatalog.map((feature) => feature.subsystem))];
  return subsystems.map((subsystem) => {
    const features = featureCatalog.filter((feature) => feature.subsystem === subsystem);
    return {
      subsystem,
      label: subsystem
        .split('-')
        .map((word) => `${word[0]?.toUpperCase() ?? ''}${word.slice(1)}`)
        .join(' '),
      features,
      operationCount: features.reduce((count, feature) => count + feature.operations.length, 0),
    };
  });
}
