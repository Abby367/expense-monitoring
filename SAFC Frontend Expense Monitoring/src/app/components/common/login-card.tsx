"use client";

import {
  Button,
  Card,
  PasswordInput,
  Stack,
  Text,
  TextInput,
  Title,
} from "@mantine/core";

export function LoginCard() {
  return (
    <Card padding="xl" w={380} withBorder>
      <Stack gap="lg">
        <Stack gap={4}>
          <Title order={3}>Sign in</Title>
          <Text size="sm" c="dimmed">
            Expense request, approval, and liquidation
          </Text>
        </Stack>

        <TextInput label="Email" placeholder="you@safc.com.ph" type="email" />
        <PasswordInput label="Password" placeholder="Your password" />

        <Button fullWidth>Sign in</Button>
      </Stack>
    </Card>
  );
}
