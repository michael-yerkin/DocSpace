# FieldContainer

## Usage

```js
import { FieldContainer } from 'asc-web-components';
```

### <Label>

Responsive form field container 

#### Usage

```js

<FieldContainer labelText="Name:">
    <TextInput/>
</FieldContainer>

```

#### Properties

| Props       | Type     | Required | Values | Default | Description                                  |
| ------------| -------- | :------: | -------| ------- | -------------------------------------------- |
| `isVertical`| `bool`   |    -     | -      | false   | Vertical or horizontal alignment             |
| `isRequired`| `bool`   |    -     | -      | false   | Indicates that the field is required to fill |
| `hasError`  | `bool`   |    -     | -      | false   | Indicates that the field is incorrect        |
| `labelText` | `string` |    -     | -      | -       | Field label text                             |