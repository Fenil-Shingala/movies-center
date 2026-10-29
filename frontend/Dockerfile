# ==============================================================================
# Multi-stage Dockerfile for MoviesCenter Frontend (Angular 22 SSR Application)
# ==============================================================================

# Stage 1: Build Angular SSR production bundles
FROM node:22-alpine AS build
WORKDIR /app

# Copy dependency manifests for layer caching
COPY package*.json ./
RUN npm ci --prefer-offline --no-audit

# Copy source code and build client + server bundles
COPY . .
RUN npm run build

# Stage 2: Minimal Node.js production runner
FROM node:22-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=4000

# Install curl for container health check
RUN apk add --no-cache curl

# Copy only the compiled SSR artifacts
COPY --from=build /app/dist/frontend ./dist/frontend
COPY package*.json ./

# Expose SSR Node server port
EXPOSE 4000

# Health check verifies the SSR server responds on root
HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:4000/ || exit 1

# Start the Angular SSR Node.js server
CMD ["node", "dist/frontend/server/server.mjs"]
