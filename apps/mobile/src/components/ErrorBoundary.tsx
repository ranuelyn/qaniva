import { Component, type ErrorInfo, type ReactNode } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, spacing, typography } from '@/theme/tokens';

interface Props {
  children: ReactNode;
}
interface State {
  error: Error | null;
}

/** Top-level crash guard so a render error shows a message instead of a white screen. */
export class ErrorBoundary extends Component<Props, State> {
  override state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  override componentDidCatch(error: Error, info: ErrorInfo): void {
    // Hook Sentry here later (blueprint §4 "observability").
    console.error('Unhandled UI error', error, info.componentStack);
  }

  override render(): ReactNode {
    if (this.state.error) {
      return (
        <View style={styles.container}>
          <Text style={styles.title}>Something went wrong</Text>
          <Text style={styles.body}>{this.state.error.message}</Text>
        </View>
      );
    }
    return this.props.children;
  }
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
    alignItems: 'center',
    justifyContent: 'center',
    padding: spacing.lg,
  },
  title: { ...typography.title, color: colors.danger, marginBottom: spacing.sm },
  body: { ...typography.body, color: colors.textMuted, textAlign: 'center' },
});
