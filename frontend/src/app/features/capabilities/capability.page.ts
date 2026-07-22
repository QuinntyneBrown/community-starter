import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { featureCatalog } from '../../core/feature-catalog.generated';

@Component({
  selector: 'cs-capability-page',
  imports: [RouterLink],
  templateUrl: './capability.page.html',
  styleUrl: './capabilities.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CapabilityPage {
  private readonly route = inject(ActivatedRoute);
  protected readonly title = String(this.route.snapshot.data['title'] ?? 'Capabilities');
  private readonly subsystem = String(this.route.snapshot.data['subsystem'] ?? '');
  protected readonly features = computed(() =>
    featureCatalog.filter((feature) => feature.subsystem === this.subsystem),
  );
  protected readonly operationCount = computed(() =>
    this.features().reduce((count, feature) => count + feature.operations.length, 0),
  );
}
