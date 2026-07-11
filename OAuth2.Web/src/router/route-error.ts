export class RouteError extends Error {
  url: string;

  constructor(url: string) {
    super(`route to ${url}`);
    this.url = url;
  }
};