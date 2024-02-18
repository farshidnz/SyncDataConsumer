import { Matcher, MatchResult } from "aws-cdk-lib/assertions";

export class StartsWithMatch extends Matcher {
  private readonly partialObjects: boolean;

  constructor(public readonly name: string, private readonly pattern: string) {
    super();

    if (Matcher.isMatcher(this.pattern)) {
      throw new Error(
        "LiteralMatch cannot directly contain another matcher. " +
          "Remove the top-level matcher or nest it more deeply."
      );
    }
  }

  public test(actual: string): MatchResult {
    const result = new MatchResult(actual);
    if (!actual.startsWith(this.pattern)) {
      result.recordFailure({
        matcher: this,
        path: [],
        message: `Cannot find matching pattern`,
      });
      return result;
    }

    return result;
  }
}
